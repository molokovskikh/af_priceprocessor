﻿using System;
using System.Linq;
using Castle.ActiveRecord;
using Common.MySql;
using Common.Tools;
using Inforoom.Common;
using Inforoom.PriceProcessor.Waybills;
using NUnit.Framework;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Properties;
using System.Threading;
using System.IO;
using MySql.Data.MySqlClient;
using Test.Support;
using Test.Support.log4net;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace PriceProcessor.Test
{
	public class WaybillLANSourceHandlerForTesting : WaybillLANSourceHandler
	{
		public void Process()
		{
			CreateDirectoryPath();
			ProcessData();
		}
	}

	[TestFixture]
	public class WaybillLANSourceHandlerFixture
	{
		private SummaryInfo _summary = new SummaryInfo();

		private const string WaybillsDirectory = @"Waybills";

		private const string RejectsDirectory = @"Rejects";

		private ulong[] _supplierCodes = new ulong[1] { 2788 };

		private string[] _waybillFiles2788 = new string[3] { "523108940_20091202030542372.zip", "523108940_20091202090615283.zip", "523108940_20091202102538565.zip" };

		[SetUp]
		public void SetUp()
		{
			TestHelper.RecreateDirectories();
			_summary.Client = TestClient.CreateSimple();
			_summary.Supplier = TestOldClient.CreateTestSupplier();
			ArchiveHelper.SevenZipExePath = Path.Combine(Path.GetFullPath("."), @"7zip\7z.exe");
		}

		private void Process_waybills()
		{
			var handler = new WaybillLANSourceHandlerForTesting();
			handler.Process();
		}

		public void Insert_waybill_source()
		{
			With.Connection(connection => {
				var command = new MySqlCommand(@"
INSERT INTO `documents`.`waybill_sources` (FirmCode, EMailFrom, SourceId, ReaderClassName) VALUES (?FirmCode, ?EmailFrom, ?SourceId, ?ReaderClassName);
UPDATE usersettings.RetClientsSet SET ParseWaybills = 1 WHERE ClientCode = ?ClientCode
", connection);
				command.Parameters.AddWithValue("?FirmCode", _summary.Supplier.Id);
				command.Parameters.AddWithValue("?EmailFrom", String.Format("{0}@test.test", _summary.Client.Id));
				command.Parameters.AddWithValue("?ClientCode", _summary.Client.Id);
				command.Parameters.AddWithValue("?ReaderClassName", "ProtekOmsk_3777_Reader");
				command.Parameters.AddWithValue("?SourceId", 4);
				command.ExecuteNonQuery();
			});
		}

		private void MaitainAddressIntersection(uint addressId)
		{
			With.Connection(connection =>
			{
				var command = new MySqlCommand(@"
insert into Future.AddressIntersection(AddressId, IntersectionId, SupplierDeliveryId)
select a.Id, i.Id, a.Id
from Future.Intersection i
	join Future.Addresses a on a.ClientId = i.ClientId
	left join Future.AddressIntersection ai on ai.AddressId = a.Id and ai.IntersectionId = i.Id
where a.Id = ?AddressId", connection);

				command.Parameters.AddWithValue("?AddressId", addressId);
				command.ExecuteNonQuery();
			});
		}

		public string Create_supplier_dir()
		{
			var directory = Path.Combine(Settings.Default.WaybillsPath, _summary.Supplier.Id.ToString());
			directory = Path.Combine(directory, DocType.Waybill + "s");

			if (Directory.Exists(directory))
				Directory.Delete(directory, true);
			Directory.CreateDirectory(directory);
			return directory;
		}

		private void CheckClientDirectory(int waitingFilesCount, DocType documentsType)
		{
			var savedFiles = GetFileForAddress(documentsType);
			Assert.That(savedFiles.Count(), Is.EqualTo(waitingFilesCount));
		}

		private string[] GetFileForAddress(DocType documentsType)
		{
			var clientDirectory = Path.Combine(Settings.Default.WaybillsPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			return Directory.GetFiles(Path.Combine(clientDirectory, documentsType + "s"), "*.*", SearchOption.AllDirectories);
		}

		private void CheckDocumentLogEntry(int waitingCountEntries)
		{
			using (new SessionScope())
			{
				var logs = TestDocumentLog.Queryable.Where(log =>
					log.ClientCode == _summary.Client.Id &&
					log.FirmCode == _summary.Supplier.Id &&
					log.AddressId == _summary.Client.Addresses[0].Id);
				Assert.That(logs.Count(), Is.EqualTo(waitingCountEntries));
			}
		}

		[Test]
		public void Parse_waybills()
		{
			var directory = Create_supplier_dir();
			var filePath = @"..\..\Data\Waybills\890579.dbf";

			File.Copy(filePath, Path.Combine(directory, String.Format("{0}_{1}", _summary.Client.Addresses[0].Id, Path.GetFileName(filePath))));
			Insert_waybill_source();
			MaitainAddressIntersection(_summary.Client.Addresses[0].Id);

			Process_waybills();

			CheckClientDirectory(1, DocType.Waybill);
			CheckDocumentLogEntry(1);
		}

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

			TestHelper.ClearImapFolder();
			TestHelper.StoreMessage(File.ReadAllBytes(@"..\..\Data\Unparse.eml"));

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

			TestHelper.ClearImapFolder();
			TestHelper.StoreMessageWithAttachToImapFolder(
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
				var logs = TestDocumentLog.Queryable.Where(d => d.ClientCode == client.Id).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));

				var documents = Document.Queryable.Where(d => d.Log.Id == logs.Single().Id).ToList();
				Assert.That(documents.Count, Is.EqualTo(1));
				Assert.That(documents.Single().Lines.Count, Is.EqualTo(7));
			}
		}

		private void Process()
		{
			var handler = new WaybillSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);
			handler.Process();
		}

		private void PrepareDirectories()
		{
			TestHelper.RecreateDirectories();

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
