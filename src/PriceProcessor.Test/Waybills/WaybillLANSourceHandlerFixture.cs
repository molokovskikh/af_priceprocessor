using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.Common;
using Inforoom.PriceProcessor.Waybills;
using log4net.Config;
using LumiSoft.Net.IMAP;
using NUnit.Framework;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Properties;
using System.Threading;
using System.IO;
using LumiSoft.Net.IMAP.Client;
using Inforoom.Downloader.Documents;
using MySql.Data.MySqlClient;
using Test.Support;
using Test.Support.log4net;


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

		[Test]
		public void Process_message_if_from_contains_more_than_one_address()
		{
			FileHelper.DeleteDir(Settings.Default.FTPOptBoxPath);

			var filter = new EventFilter<WaybillSourceHandler>();

			TestHelper.StoreMessage(Settings.Default.TestIMAPUser,
				Settings.Default.TestIMAPPass,
				Settings.Default.IMAPSourceFolder,
				File.ReadAllBytes(@"..\..\Data\Unparse.eml"));

			Process();

			var ftp = Path.Combine(Settings.Default.FTPOptBoxPath, @"4147\rejects\");
			Assert.That(Directory.Exists(ftp), "не обработали документ");
			Assert.That(Directory.GetFiles(ftp).Length, Is.EqualTo(1));

			Assert.That(filter.Events.Count, Is.EqualTo(0), "во премя обработки произошли ошибки, {0}", filter.Events.Implode(m => m.ExceptionObject.ToString()));
		}

		[Test]
		public void Parse_waybill_if_parsing_enabled()
		{
			var filter = new EventFilter<WaybillSourceHandler>();

			var client = TestOldClient.CreateTestClient();
			var settings = WaybillSettings.Find(client.Id);
			settings.ParseWaybills = true;
			settings.Update();
			TestHelper.StoreMessageWithAttachToImapFolder(Settings.Default.TestIMAPUser,
				Settings.Default.TestIMAPPass,
				Settings.Default.IMAPSourceFolder,
				String.Format("{0}@waybills.analit.net", client.Id),
				"edata@msk.katren.ru",
				@"..\..\Data\Waybills\8916.dbf");

			Process();

			Assert.That(filter.Events.Count, Is.EqualTo(0), "Ошибки {0}", filter.Events.Implode(e => e.ExceptionObject.ToString()));

			var ftp = Path.Combine(Settings.Default.FTPOptBoxPath, String.Format(@"{0}\waybills\", client.Id));
			Assert.That(Directory.Exists(ftp), "не обработали документ");
			Assert.That(Directory.GetFiles(ftp).Length, Is.EqualTo(1));

			using(new SessionScope())
			{
				var logs = TestDocument.Queryable.Where(d => d.ClientCode == client.Id).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));

				var documents = Document.Queryable.Where(d => d.Log.Id == logs.Single().Id).ToList();
				Assert.That(documents.Count, Is.EqualTo(1));
				Assert.That(documents.Single().Lines.Count, Is.EqualTo(7));
			}
		}

		private void Process()
		{
			var handler = new WaybillSourceHandler(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);
			handler.StartWork();
			Thread.Sleep(5000);
			handler.StopWork();
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
