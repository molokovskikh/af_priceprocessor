using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LumiSoft.Net.IMAP;
using NUnit.Framework;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Properties;
using System.Threading;
using System.IO;
using LumiSoft.Net.IMAP.Client;
using Inforoom.Downloader.Documents;
using MySql.Data.MySqlClient;


namespace PriceProcessor.Test
{
	[TestFixture]
	public class WaybillLANSourceHandlerFixture
	{
		private const string WaybillsDirectory = @"Waybills";

		private const string RejectsDirectory = @"Rejects";

		private ulong[] _supplierCodes = new ulong[1] { 2788 };

		private string[] _waybillFiles2788 = new string[3] { "523108940_20091202030542372.zip", "523108940_20091202090615283.zip", "523108940_20091202102538565.zip" };


		[Test]
		public void TestSIAMoscow2788()
		{
			var supplierCode = 2788;

            PrepareDirectories();

			CopyFilesFromDataDirectory(_waybillFiles2788, supplierCode);

			ClearDocumentHeadersTable(Convert.ToUInt64(supplierCode));

			// Запускаем обработчик
			var handler = new WaybillLANSourceHandler();
			handler.StartWork();
			// Ждем какое-то время, чтоб обработчик обработал
			Thread.Sleep(5000);
			handler.StopWork();

			var path = Path.GetFullPath(Settings.Default.FTPOptBoxPath);
			var clientDirectories = Directory.GetDirectories(Path.GetFullPath(Settings.Default.FTPOptBoxPath));
			Assert.IsTrue(clientDirectories.Length > 1, "Не создано ни одной директории для клиента-получателя накладной " + path + " " + clientDirectories.Length);
		}

		private void PrepareDirectories()
		{
			TestHelper.RecreateDirectories();

			// Удаляем папку FtpOptBox
			if (Directory.Exists(Settings.Default.FTPOptBoxPath))
				Directory.Delete(Settings.Default.FTPOptBoxPath, true);

			// Создаем ее заново и копируем туда накладные. Пока только для SIA (код 2788)
			// Потом можно будет сюда добавить других поставщиков
			Directory.CreateDirectory(Settings.Default.FTPOptBoxPath);

			// Создаем директории для поставщиков 
			foreach (var supplierCode in _supplierCodes)
			{
				var supplierDir = Settings.Default.FTPOptBoxPath + Path.DirectorySeparatorChar +
				                  Convert.ToString(supplierCode) + Path.DirectorySeparatorChar;
				Directory.CreateDirectory(supplierDir);
				Directory.CreateDirectory(supplierDir + WaybillsDirectory);
				Directory.CreateDirectory(supplierDir + RejectsDirectory);
			}
		}

		private void CopyFilesFromDataDirectory(string[] fileNames, int supplierCode)
		{
			var dataDirectory = Path.GetFullPath(Settings.Default.TestDataDirectory);
			var supplierDirectory = Path.GetFullPath(Settings.Default.FTPOptBoxPath) + Path.DirectorySeparatorChar + supplierCode +
									Path.DirectorySeparatorChar + WaybillsDirectory + Path.DirectorySeparatorChar;
			// Копируем файлы в папку поставщика
			foreach (var fileName in fileNames)
				File.Copy(dataDirectory + fileName, supplierDirectory + fileName);
		}

		private void ClearDocumentHeadersTable(ulong supplierCode)
		{
			var queryDelete = @"
DELETE FROM documents.DocumentHeaders
WHERE FirmCode = ?SupplierId
";
			var paramSupplierId = new MySqlParameter("?SupplierId", supplierCode);
			With.Connection(connection => { MySqlHelper.ExecuteNonQuery(connection, queryDelete, paramSupplierId); });
		}
	}
}
