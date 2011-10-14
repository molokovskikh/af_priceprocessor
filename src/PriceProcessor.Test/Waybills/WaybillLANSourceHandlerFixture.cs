﻿using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using Castle.ActiveRecord;
using Common.MySql;
using Common.Tools;
using Inforoom.Downloader.DocumentReaders;
using Inforoom.Downloader.Documents;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net;
using log4net.Appender;
using log4net.Config;
using NUnit.Framework;
using Inforoom.Downloader;
using Inforoom.PriceProcessor;
using System.IO;
using MySql.Data.MySqlClient;
using Test.Support;
using Test.Support.log4net;
using Test.Support.Suppliers;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;
using WaybillSourceType = Test.Support.WaybillSourceType;
using FileHelper = Inforoom.Common.FileHelper;

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

		private string[] _waybillFiles2788 = new string[3] { "826874436_20091202030542372.zip", "826874436_20091202090615283.zip", "826874436_20091202102538565.zip" };
		
		[SetUp]
		public void SetUp()
		{
			TestHelper.RecreateDirectories();
			_summary.Client = TestClient.Create();
			_summary.Supplier = TestSupplier.Create();
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
			var directory = Path.Combine(Settings.Default.FTPOptBoxPath, _summary.Supplier.Id.ToString());
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
			var clientDirectory = Path.Combine(Settings.Default.DocumentPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
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
		public void Parse_waybills_Convert_Dbf_format()
		{
			var directory = Create_supplier_dir();
			var filePath = @"..\..\Data\Waybills\890579.dbf";

			File.Copy(filePath, Path.Combine(directory, String.Format("{0}_{1}", _summary.Client.Addresses[0].Id, Path.GetFileName(filePath))));
			Insert_waybill_source();
			MaitainAddressIntersection(_summary.Client.Addresses[0].Id);

			var settings = TestDrugstoreSettings.Queryable.Where(s => s.Id == _summary.Client.Id).SingleOrDefault();
			//запоминаем начальное состояние настройки
			var source_IsConvertFormat = settings.IsConvertFormat;
			//и если оно не включено, то включим принудительно для теста
			if (!source_IsConvertFormat)
			{
				using (new TransactionScope())
				{
					settings.IsConvertFormat = true;
					settings.AssortimentPriceId = (int)Core.Queryable.First().Price.Id;
					settings.SaveAndFlush();
				}
			}

			Process_waybills();

			CheckClientDirectory(1, DocType.Waybill);

			using (new SessionScope())
			{
				var logs = TestDocumentLog.Queryable.Where(log =>
					log.ClientCode == _summary.Client.Id &&
					log.FirmCode == _summary.Supplier.Id &&
					log.AddressId == _summary.Client.Addresses[0].Id);
				
				Assert.That(logs.Count(), Is.EqualTo(2));
				Assert.That(logs.Where(l => l.IsFake).Count(), Is.EqualTo(1));
				Assert.That(logs.Where(l => !l.IsFake).Count(), Is.EqualTo(1));
				
				var _log = logs.Where(l => !l.IsFake).SingleOrDefault();
				var file_dbf = GetFileForAddress(DocType.Waybill).Where(f => f.IndexOf(_log.FileName) > -1).SingleOrDefault();
				
				var data = Dbf.Load(file_dbf, Encoding.GetEncoding(866));
				Assert.IsTrue(data.Columns.Contains("postid_af"));
				Assert.IsTrue(data.Columns.Contains("ttn"));
				Assert.IsTrue(data.Columns.Contains("przv_post"));
			}

			//если было включено принудительно, то вернем назад настройку.
			if (!source_IsConvertFormat)
			{
				using (new TransactionScope())
				{
					settings.IsConvertFormat = false;
					settings.AssortimentPriceId = null;
				}
			}
		}


		[Test]
		public void TestSIAMoscow2788()
		{            
			var supplierCode = 2788;

			PrepareDirectories();

			CopyFilesFromDataDirectory(_waybillFiles2788, supplierCode);

			ClearDocumentHeadersTable(Convert.ToUInt64(supplierCode));

			Process_waybills();
		
			var path = Path.GetFullPath(Settings.Default.DocumentPath);
			var clientDirectories = Directory.GetDirectories(Path.GetFullPath(Settings.Default.DocumentPath));
			Assert.IsTrue(clientDirectories.Length > 1, "Не создано ни одной директории для клиента-получателя накладной " + path + " " + clientDirectories.Length);
		}
 
		[Test]
		public void Process_message_if_from_contains_more_than_one_address()
		{
			FileHelper.DeleteDir(Settings.Default.DocumentPath);

			var filter = new EventFilter<WaybillSourceHandler>();

			TestHelper.ClearImapFolder();
			TestHelper.StoreMessage(File.ReadAllBytes(@"..\..\Data\Unparse.eml"));

			Process();

			var ftp = Path.Combine(Settings.Default.DocumentPath, @"4147\rejects\");
			Assert.That(Directory.Exists(ftp), "не обработали документ");
			Assert.That(Directory.GetFiles(ftp).Length, Is.EqualTo(1));

			Assert.That(filter.Events.Count, Is.EqualTo(0), "во премя обработки произошли ошибки, {0}", filter.Events.Implode(m => m.ExceptionObject.ToString()));
		}

		[Test]
		public void Parse_waybill_if_parsing_enabled()
		{
			var appender = new ConsoleAppender
			{
				Layout = new SimpleLayout1()
			};

			appender.ActivateOptions();
			BasicConfigurator.Configure(appender);

			try
			{
				var beign = DateTime.Now;
				var filter = new EventFilter<WaybillSourceHandler>();
				var supplier = TestSupplier.Create();
				var client = TestClient.Create();

				const string email = "edata@msk.katren.ru";
				uint firmId = 0;

				var query = string.Format("select FirmCode from documents.waybill_sources where EMailFrom LIKE '%{0}%' LIMIT 1;", email);
				With.Connection(connection => { uint.TryParse(MySqlHelper.ExecuteScalar(connection, query).ToString(), out firmId); });

				using (new SessionScope())
				{

					if (firmId == 0)
					{
						var waybillsource = new TestWaybillSource() {EMailFrom = email, SourceType = WaybillSourceType.Email};
						waybillsource.Id = supplier.Id;
						waybillsource.Create();
					}

					//
					var settings = WaybillSettings.Find(client.Id);
					settings.ParseWaybills = true;
					settings.Update();

				}

				TestHelper.ClearImapFolder();
				TestHelper.StoreMessageWithAttachToImapFolder(
					String.Format("{0}@waybills.analit.net", client.Addresses[0].Id),
					email,
					@"..\..\Data\Waybills\8916.dbf");

				Process();

				Assert.That(filter.Events.Count, Is.EqualTo(0), "Ошибки {0}", filter.Events.Implode(e => e.ExceptionObject.ToString()));

				var ftp = Path.Combine(Settings.Default.DocumentPath, String.Format(@"{0}\waybills\", client.Addresses[0].Id));
				Assert.That(Directory.Exists(ftp), "не обработали документ");
				Assert.That(Directory.GetFiles(ftp).Length, Is.EqualTo(1));

				using (new SessionScope())
				{
					var logs = TestDocumentLog.Queryable.Where(d => d.ClientCode == client.Id).ToList();
					Assert.That(logs.Count, Is.EqualTo(1));
					var log = logs.Single();
					Assert.That(log.LogTime, Is.GreaterThanOrEqualTo(beign));
					Assert.That(log.DocumentSize, Is.GreaterThan(0));

					var documents = Document.Queryable.Where(d => d.Log.Id == log.Id).ToList();
					Assert.That(documents.Count, Is.EqualTo(1));
					Assert.That(documents.Single().Lines.Count, Is.EqualTo(7));
				}
			}
			finally
			{
				LogManager.ResetConfiguration();
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




	public class FakeSIAMoscow_2788_Reader1 : SIAMoscow_2788_Reader
	{		
		public override List<ulong> GetClientCodes(MySqlConnection Connection, ulong FirmCode, string ArchFileName, string CurrentFileName)
		{
			throw new Exception("Не получилось сформировать SupplierClientId(FirmClientCode) и SupplierDeliveryId(FirmClientCode2) из документа.");
		}
	}

	public class FakeSIAMoscow_2788_Reader2 : SIAMoscow_2788_Reader
	{
		public override List<ulong> GetClientCodes(MySqlConnection Connection, ulong FirmCode, string ArchFileName, string CurrentFileName)
		{
			return null;
		}
		public override string FormatOutputFile(string InputFile, DataRow drSource)
		{
			throw new Exception("Количество позиций в документе не соответствует значению в заголовке документа");
		}
	}

	public class FakeSIAMoscow_2788_Reader3 : SIAMoscow_2788_Reader
	{
		public override List<ulong> GetClientCodes(MySqlConnection Connection, ulong FirmCode, string ArchFileName, string CurrentFileName)
		{
			var res = new List<ulong>();
			res.AddRange(new ulong [] {0});
			return res;
		}
		public override string FormatOutputFile(string InputFile, DataRow drSource)
		{
			return "test";
		}
		public override void ImportDocument(DocumentReceiveLog log, string filename)
		{
			throw new Exception("Дублирующийся документ");
		}

	}

	public class FakeSIAMoscow_2788_Reader4 : SIAMoscow_2788_Reader
	{
		public override List<ulong> GetClientCodes(MySqlConnection Connection, ulong FirmCode, string ArchFileName, string CurrentFileName)
		{
			var res = new List<ulong>();
			res.AddRange(new ulong[] { 0 });
			return res;
		}
		public override string FormatOutputFile(string InputFile, DataRow drSource)
		{
			return "test";
		}
		public override void ImportDocument(DocumentReceiveLog log, string filename)
		{
			using (var transaction = new TransactionScope(OnDispose.Rollback))
			{
				log.Save();
				transaction.VoteCommit();
			}
		}
	}

	public class FakeWaybillLANSourceHandler : WaybillLANSourceHandler
	{
		private readonly DataRow drLanSource;
		private readonly BaseDocumentReader reader;

		public  FakeWaybillLANSourceHandler()
		{}

		public FakeWaybillLANSourceHandler(string readerClassName)
		{
			FillSourcesTable();
			drLanSource = dtSources.Rows.Cast<DataRow>().Where(r => r["ReaderClassName"].ToString() == "SIAMoscow_2788_Reader").FirstOrDefault();			
			Type result = null;
			var types = Assembly.GetExecutingAssembly()
								.GetModules()[0]
								.FindTypes(Module.FilterTypeNameIgnoreCase, readerClassName);
			result = types[0];
			reader = (BaseDocumentReader)Activator.CreateInstance(result);
			_currentDocumentType = new WaybillType();
		}

		public bool MoveWaybill(string archFileName, string fileName)
		{
			return base.MoveWaybill(archFileName, fileName, drLanSource, reader);
		}
	}

	[TestFixture]
	public class WaybillLANSourceHandlerErrorsFixture
	{
		private DateTime start;
		private int maxLogId;

		[SetUp]
		public void Setup()
		{
			using (new SessionScope())
			{
				maxLogId = DocumentReceiveLog.Queryable.Max(l => (int)l.Id);	
			}
			start = DateTime.Now;
		}

		[TearDown]
		public void EndTest()
		{
			With.Connection(connection =>
			{
				var command = new MySqlCommand(@"delete from logs.document_logs where rowid > ?Id", connection);
				command.Parameters.AddWithValue("?Id", maxLogId);
				command.ExecuteNonQuery();
			});
		}

		[Test]
		public void GetClientCodesErrorTest()
		{
			var handler = new FakeWaybillLANSourceHandler("FakeSIAMoscow_2788_Reader1");
			var res = handler.MoveWaybill("test", "test");			
			using(new SessionScope())
			{
				Assert.That(res, Is.False);
				var logs = DocumentReceiveLog.Queryable.Where(l => l.LogTime >= start).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));
				Assert.That(logs[0].Supplier.Id, Is.EqualTo(2788));
				Assert.That(logs[0].Comment.Contains("Не получилось сформировать SupplierClientId(FirmClientCode) и SupplierDeliveryId(FirmClientCode2) из документа."), Is.True);
			}
		}

		[Test]
		public void FormatOutputFileError()
		{			
			var handler = new FakeWaybillLANSourceHandler("FakeSIAMoscow_2788_Reader2");
			var res = handler.MoveWaybill("test", "test");
			using (new SessionScope())
			{
				Assert.That(res, Is.False);
				var logs = DocumentReceiveLog.Queryable.Where(l => l.LogTime >= start).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));
				Assert.That(logs[0].Supplier.Id, Is.EqualTo(2788));
				Assert.That(logs[0].Comment.Contains("Количество позиций в документе не соответствует значению в заголовке документа"), Is.True);
			}
		}

		[Test]
		public void ImportDocumentError()
		{			
			var handler = new FakeWaybillLANSourceHandler("FakeSIAMoscow_2788_Reader3");
			var res = handler.MoveWaybill("test", "test");
			using (new SessionScope())
			{
				Assert.That(res, Is.False);
				var logs = DocumentReceiveLog.Queryable.Where(l => l.LogTime >= start).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));
				Assert.That(logs[0].Supplier.Id, Is.EqualTo(2788));
				Assert.That(logs[0].Comment.Contains("Дублирующийся документ"), Is.True);
			}
		}

		[Test]
		public void WithoutError()
		{			
			var handler = new FakeWaybillLANSourceHandler("FakeSIAMoscow_2788_Reader4");
			var res = handler.MoveWaybill("test", "test");
			using (new SessionScope())
			{
				Assert.That(res, Is.True);
				var logs = DocumentReceiveLog.Queryable.Where(l => l.LogTime >= start).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));
				Assert.That(logs[0].Supplier.Id, Is.EqualTo(2788));
				Assert.That(logs[0].Comment, Is.Null);
			}
		}
	}
}
