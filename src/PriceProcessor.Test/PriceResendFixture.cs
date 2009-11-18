using System;
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
		private static uint[] DownlogIds = new uint[4] { 6905857, 6905845, 6905885, 6906022 };

		// Массив имен файлов (для каждого идентификатора должно быть имя на той же позиции в массиве)
		private static string[] FileNames = new string[4] { "6905857.eml", "6905845.zip", "6905885.eml", "6906022.xls" };

		[TestFixtureSetUp, Description("Создание нужных папок, проверка, что папки с нужными файлами существуют")]
		public void TestPrepareDirectories()
		{
			// Удаляем папку Inbound со всеми файлами в ней,
			// чтобы не возникала ошибка о том, что прайс находится на формализации
			if (Directory.Exists(Settings.Default.InboundPath))
				Directory.Delete(Settings.Default.InboundPath, true);
			if (Directory.Exists(Settings.Default.HistoryPath))
				Directory.Delete(Settings.Default.HistoryPath, true);
			Program.InitDirs(new[]
				         	{
				         		Settings.Default.BasePath,
				         		Settings.Default.ErrorFilesPath,
				         		Settings.Default.InboundPath,
				         		Settings.Default.TempPath,
				         		Settings.Default.HistoryPath
				         	});
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

		[Test, Description("Тест для перепосылки прайса")]
		public void TestResendPrice()
		{
			const string queryGetPriceItemIdByDownlogId = @"
SELECT d.PriceItemId
FROM logs.downlogs d
WHERE RowId = ?downlogId
";
			int index = 0;
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

		private void WcfCallResendPrice(uint downlogId)
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
				priceProcessor.ResendPrice(downlogId);
				((ICommunicationObject)priceProcessor).Close();
				success = true;
			}
			catch (FaultException faultEx)
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
