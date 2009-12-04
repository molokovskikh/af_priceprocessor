﻿using System;
using System.Text;
using NUnit.Framework;
using System.IO;
using System.ServiceModel;
using System.Net;
using Inforoom.PriceProcessor.Properties;
using Inforoom.PriceProcessor;
using System.Net.Security;
using RemotePriceProcessor;
using MySql.Data.MySqlClient;

namespace PriceProcessor.Test
{
	[TestFixture, Description("Тест для перепосылки прайса")]
	public class PriceResendFixture
	{
		private ServiceHost _serviceHost;

		// Массив идентификаторов RowId из таблицы logs.downlogs
		private static ulong[] DownlogIds = new ulong[5] { 6905857, 6905845, 6905885, 6906022, 6906021 };

		// Массив идентификаторов PriceItemId из таблицы logs.downlogs
		private static ulong[] PriceItemIds = new ulong[5] { 1006, 969, 648, 747, 748 };

		private static ulong[] SourceIds = new ulong[5] { 4950, 4951, 4952, 4953, 4952 };

		private static ulong[] SourceTypeIds = new ulong[5] { 1, 3, 1, 4, 3 }; 

		// Массив имен файлов (для каждого идентификатора должно быть имя на той же позиции в массиве)
		private static string[] FileNames = new string[5] { "6905857.eml", "6905845.zip", "6905885.eml", "6906022.xls", "6906021.exe" };

		private static string[] ArchFileNames = new string[5] { "price.zip", "price2.zip", "prs.txt", "price4.xls", "price.exe" };

		private static string[] ExtrFileNames = new string[5] { "price.txt", "price-15.09.2009.xls", "prs.txt", "price4.xls", "price.txt" };

		private static string[] ArchivePasswords = new string[5] { "123", "", "", "", "rar1" };

		[TestFixtureSetUp, Description("Создание нужных папок, проверка, что папки с нужными файлами существуют")]
		public void InitFixture()
		{
			PrepareDirectories();
			PrepareDownlogsTable();
			PrepareSourcesTable();
		}

		private void PrepareDirectories()
		{
			TestHelper.RecreateDirectories();
			var directory = Path.GetFullPath(@"..\..\Data");
			if (Directory.Exists(directory))
			{
				foreach (var fileName in FileNames)
				{
					var files = Directory.GetFiles(directory, fileName);
					if (files.Length <= 0)
					{
						Assert.Fail(String.Format("Не найден файл {0} в директории {1}",
						                          fileName, directory));
						break;
					}
					try
					{
						// Копируем файл в DownHistory
						File.Copy(directory + "\\" + fileName, Settings.Default.HistoryPath + "\\" + fileName);
					}
					catch (IOException)
					{}
				}
			}
			Assert.IsTrue(true);
		}

		private void PrepareDownlogsTable()
		{
			const string queryInsert = @"
INSERT INTO logs.downlogs
VALUES(?DownlogId, NOW(), ?PriceItemId, ""TEST"", NULL, 2, ?ArchFileName, ?ExtrFileName)
";
			const string queryUpdate = @"
UPDATE logs.downlogs
SET PriceItemId = ?PriceItemId, ArchFileName = ?ArchFileName, ExtrFileName = ?ExtrFileName
WHERE RowId = ?DownlogId
";
			for (var index = 0; index < DownlogIds.Length; index++)
			{
				var paramDownlogId = new MySqlParameter("?DownlogId", DownlogIds[index]);
				var paramPriceItemId = new MySqlParameter("?PriceItemId", PriceItemIds[index]);
				var paramArchFileName = new MySqlParameter("?ArchFileName", ArchFileNames[index]);
				var paramExtrFileName = new MySqlParameter("?ExtrFileName", ExtrFileNames[index]);

				TestHelper.InsertOrUpdateTable(queryInsert, queryUpdate, paramDownlogId, paramPriceItemId, paramArchFileName,
				                               paramExtrFileName);
			}
		}

		// Вставляет/обновляет нужные записи в таблице farm.sources
		private void PrepareSourcesTable()
		{
			PreparePriceItemsTable();

			var queryInsert = @"
INSERT INTO farm.sources
VALUES(?SourceId, ?SourceTypeId, NULL, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, ?ArchivePassword, NULL, NULL)
";
			var queryUpdate = @"
UPDATE farm.sources
SET SourceTypeId = ?SourceTypeId, ArchivePassword = ?ArchivePassword
WHERE Id = ?SourceId
";
            for (var index = 0; index < SourceIds.Length; index++)
            {
            	var paramSourceId = new MySqlParameter("?SourceId", SourceIds[index]);
            	var paramSourceTypeId = new MySqlParameter("?SourceTypeId", SourceTypeIds[index]);
            	var paramArchivePassword = new MySqlParameter("?ArchivePassword", ArchivePasswords[index]);

				TestHelper.InsertOrUpdateTable(queryInsert, queryUpdate, paramSourceId, paramSourceTypeId, paramArchivePassword);
            }
		}

		// Вставляет/обновляет нужные записи в таблице usersettings.PriceItems
		private void PreparePriceItemsTable()
		{
			var queryInsert = @"
INSERT INTO usersettings.PriceItems
VALUES(?PriceItemId, 2, ?SourceId, 0, 0, NOW(), NOW(), NULL, NULL, NULL, 1)
";
			var queryUpdate = @"
UPDATE usersettings.PriceItems
SET SourceId = ?SourceId
WHERE Id = ?PriceItemId
";
			for (var index = 0; index < PriceItemIds.Length; index++)
			{
				var paramPriceItemId = new MySqlParameter("?PriceItemId", PriceItemIds[index]);
				var paramSourceId = new MySqlParameter("?SourceId", SourceIds[index]);

				TestHelper.InsertOrUpdateTable(queryInsert, queryUpdate, paramPriceItemId, paramSourceId);
			}
		}

		[Test, Description("Тест для перепосылки прайса")]
		public void TestResendPrice()
		{
			const string queryGetPriceItemIdByDownlogId = @"
SELECT d.PriceItemId
FROM logs.downlogs d
WHERE RowId = ?downlogId
";
			var priceInQuery = false;
			StartWcfPriceProcessor();
			foreach (var downlogId in DownlogIds)
			{
				// Перепроводим прайс
				WcfCallResendPrice(downlogId);

				var paramDownlogId = new MySqlParameter("?downlogId", downlogId);
				// Получаем priceItemId по downlogId
				var priceItemId = With.Connection<ulong>(connection => {
					return Convert.ToUInt64(MySqlHelper.ExecuteScalar(
						connection, queryGetPriceItemIdByDownlogId, paramDownlogId));
				});
				// Смотрим, есть ли только что проведенный priceItemId в очереди на формализацию
				foreach (var item in PriceItemList.list)
				{
					if (item.PriceItemId == priceItemId)
					{
						priceInQuery = true;
						break;
					}
				}
				// Если прайса в очереди нет, ошибка
				if (!priceInQuery)
					Assert.Fail(
						"Прайса c PriceItemId = {0} и downlogId = {1} нет в очереди на формализацию",
						priceItemId, downlogId);
				priceInQuery = false;
			}
			StopWcfPriceProcessor();
		}

		private void WcfCallResendPrice(ulong downlogId)
		{
			const string _strProtocol = @"net.tcp://";
			var binding = new NetTcpBinding();
			binding.Security.Transport.ProtectionLevel = ProtectionLevel.EncryptAndSign;
			binding.Security.Mode = SecurityMode.None;
			// Ипользуется потоковая передача данных в обе стороны 
			binding.TransferMode = TransferMode.Streamed;
			// Максимальный размер принятых данных
			binding.MaxReceivedMessageSize = Int32.MaxValue;
			// Максимальный размер одного пакета
			binding.MaxBufferSize = 524288;    // 0.5 Мб
			StringBuilder sbUrlService = new StringBuilder();
			sbUrlService.Append(_strProtocol)
                .Append(Dns.GetHostName()).Append(":")
                .Append(Settings.Default.WCFServicePort).Append("/")
                .Append(Settings.Default.WCFServiceName);
			var factory = new ChannelFactory<IRemotePriceProcessor>(binding, sbUrlService.ToString());
			IRemotePriceProcessor priceProcessor = factory.CreateChannel();
			var success = false;
			try
			{
				var paramDownlogId = new WcfCallParameter() {
                    Value = downlogId,
                    LogInformation = new LogInformation() {
                        ComputerName = Environment.MachineName,
                        UserName = Environment.UserName
                    }
                };
				priceProcessor.ResendPrice(paramDownlogId);
				((ICommunicationObject)priceProcessor).Close();
				success = true;
			}
			catch (FaultException)
			{
				if (((ICommunicationObject)priceProcessor).State != CommunicationState.Closed)
					((ICommunicationObject)priceProcessor).Abort();
				throw;
			}
			factory.Close();
			Assert.IsTrue(success);
		}

		private void StartWcfPriceProcessor()
		{
			const string _strProtocol = @"net.tcp://";
			StringBuilder sbUrlService = new StringBuilder();
			_serviceHost = new ServiceHost(typeof(WCFPriceProcessorService));
			sbUrlService.Append(_strProtocol)
				.Append(Dns.GetHostName()).Append(":")
				.Append(Settings.Default.WCFServicePort).Append("/")
				.Append(Settings.Default.WCFServiceName);
			NetTcpBinding binding = new NetTcpBinding();
			binding.Security.Transport.ProtectionLevel = ProtectionLevel.EncryptAndSign;
			binding.Security.Mode = SecurityMode.None;
			// Ипользуется потоковая передача данных в обе стороны 
			binding.TransferMode = TransferMode.Streamed;
			// Максимальный размер принятых данных
			binding.MaxReceivedMessageSize = Int32.MaxValue;
			// Максимальный размер одного пакета
			binding.MaxBufferSize = 524288;    // 0.5 Мб 
			_serviceHost.AddServiceEndpoint(typeof(IRemotePriceProcessor), binding,
				sbUrlService.ToString());
			_serviceHost.Open();
		}

		private void StopWcfPriceProcessor()
		{
			try
			{
				_serviceHost.Close();
			}
			catch (Exception)
			{}
		}
	}
}
