using System;
using System.Collections.Generic;
using System.Linq;
using Common.MySql;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills;
using LumiSoft.Net.Mime;
using NUnit.Framework;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Properties;
using System.IO;
using MySql.Data.MySqlClient;
using Test.Support;
using Test.Support.Suppliers;
using Castle.ActiveRecord;
using LumiSoft.Net;

namespace PriceProcessor.Test
{
	public class SummaryInfo
	{
		public TestClient Client { get; set; }

		//public TestOldClient Supplier { get; set; }
		public TestSupplier Supplier { get; set; }
	}

	public class WaybillSourceHandlerForTesting : WaybillSourceHandler
	{
		public bool Result = false;

		public WaybillSourceHandlerForTesting(string mailbox, string password)
			: base(mailbox, password)
		{
		}

		public void Process()
		{
			CreateDirectoryPath();
			ProcessData();
		}

		public void CheckMimeTest(Mime m)
		{
			CheckMime(m);
		}

		protected override void ErrorOnMessageProcessing(Mime m, AddressList from, EMailSourceHandlerException e)
		{
			if (e is EmailFromUnregistredMail)
			{
				Result = true;
			}
		}
	}

	[TestFixture]
	public class WaybillSourceHandlerFixture
	{
		private IList<string> _fileNames = new List<string> {@"..\..\Data\Waybills\0000470553.dbf"};

		private SummaryInfo _summary = new SummaryInfo();

		private bool IsEmlFile = false;

		[SetUp]
		public void DeleteDirectories()
		{
			TestHelper.RecreateDirectories();
		}

		private static void CreateWaybillSource(TestClient client, TestSupplier supplier)
		{
			With.Connection(connection =>
			{
				var command = new MySqlCommand(@"
INSERT INTO `documents`.`waybill_sources` (FirmCode, EMailFrom, SourceId) VALUES (?FirmCode, ?EmailFrom, ?SourceId);
UPDATE usersettings.RetClientsSet SET ParseWaybills = 1 WHERE ClientCode = ?ClientCode
", connection);
				command.Parameters.AddWithValue("?FirmCode", supplier.Id);
				command.Parameters.AddWithValue("?EmailFrom", String.Format("{0}@test.test", client.Id));
				command.Parameters.AddWithValue("?ClientCode", client.Id);
				command.Parameters.AddWithValue("?SourceId", 1);
				command.ExecuteNonQuery();
			});
		}

		public void SetUp(IList<string> fileNames)
		{
			TestHelper.RecreateDirectories();

			//var client = TestClient.CreateSimple();
			//var supplier = TestOldClient.CreateTestSupplier();
			var client = TestClient.Create();
			var supplier = TestSupplier.Create();

			CreateWaybillSource(client, supplier);
			TestHelper.ClearImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);

			byte[] bytes;
			if (IsEmlFile)
				bytes = File.ReadAllBytes(fileNames[0]);
			else
			{
				var message = TestHelper.BuildMessageWithAttachments(
					String.Format("{0}@waybills.analit.net", client.Addresses[0].Id),
					String.Format("{0}@test.test", client.Id), fileNames.ToArray());
				bytes = message.ToByteData();
			}

			TestHelper.StoreMessage(
				Settings.Default.TestIMAPUser,
				Settings.Default.TestIMAPPass,
				Settings.Default.IMAPSourceFolder, bytes);
			_summary.Client = client;
			_summary.Supplier = supplier;
		}

		private void CheckClientDirectory(int waitingFilesCount, DocType documentsType)
		{
			//var clientDirectory = Path.Combine(Settings.Default.FTPOptBoxPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var clientDirectory = Path.Combine(Settings.Default.DocumentPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var savedFiles = Directory.GetFiles(Path.Combine(clientDirectory, documentsType + "s"), "*.*", SearchOption.AllDirectories);
			Assert.That(savedFiles.Count(), Is.EqualTo(waitingFilesCount));			
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

		private void CheckDocumentEntry(int waitingCountEntries)
		{
			using (new SessionScope())
			{
				var documents = Document.Queryable.Where(doc => doc.FirmCode == _summary.Supplier.Id &&
					doc.ClientCode == _summary.Client.Id &&
					doc.AddressId == _summary.Client.Addresses[0].Id);
				Assert.That(documents.Count(), Is.EqualTo(waitingCountEntries));
			}			
		}

		//private void Process_waybills()
		private WaybillSourceHandlerForTesting Process_waybills()
		{			
			WaybillSourceHandlerForTesting handler = new WaybillSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);
			handler.Process();
			return handler;
		}

		[Test, Description("Проверка вставки даты документа после разбора накладной")]
		public void Check_document_date()
		{
			SetUp(new List<string> {@"..\..\Data\Waybills\0000470553.dbf"});
			Process_waybills();
			using (new SessionScope())
			{
				var documents = Document.Queryable.Where(doc => doc.FirmCode == _summary.Supplier.Id &&
					doc.ClientCode == _summary.Client.Id &&
					doc.AddressId == _summary.Client.Addresses[0].Id &&
					doc.DocumentDate != null);
				Assert.That(documents.Count(), Is.EqualTo(1));
			}
		}

		[Test, Description("Проверка, что накладная скопирована в папку клиенту")]
		public void Check_copy_waybill_to_client_dir()
		{
			var fileNames = new List<string> { @"..\..\Data\Waybills\0000470553.dbf" };
			SetUp(fileNames);
			Process_waybills();

			//var clientDirectory = Path.Combine(Settings.Default.FTPOptBoxPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var clientDirectory = Path.Combine(Settings.Default.DocumentPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var savedFiles = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*(0000470553).dbf",
				SearchOption.AllDirectories);
			Assert.That(savedFiles.Count(), Is.EqualTo(1));

			var tempFilePath = Path.Combine(Settings.Default.TempPath, "DownWAYBILL");
			tempFilePath = Path.Combine(tempFilePath, "0000470553.dbf");
			Assert.IsFalse(File.Exists(tempFilePath));
		}

		[Test, Description("Тест для случая когда два поставщика имеют один и тот же email в Waybill_wources (обычно это филиалы одного и того же поставщика)")]
		public void Send_waybill_from_supplier_with_filials()
		{
			var fileNames = new List<string> { @"..\..\Data\Waybills\0000470553.dbf" };
			SetUp(fileNames);

		//	var supplier = TestOldClient.CreateTestSupplier(64UL);
			var supplier = TestSupplier.Create(64UL);
			CreateWaybillSource(_summary.Client, supplier);
			Process_waybills();

			//var clientDirectory = Path.Combine(Settings.Default.FTPOptBoxPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var clientDirectory = Path.Combine(Settings.Default.DocumentPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var savedFiles = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*(0000470553).dbf",
				SearchOption.AllDirectories);
			Assert.That(savedFiles.Count(), Is.EqualTo(1));
		}

		[Test]
		public void Parse_multifile_document()
		{
			var files = new List<string> {
				@"..\..\Data\Waybills\multifile\b271433.dbf",
				@"..\..\Data\Waybills\multifile\h271433.dbf"
			};
			SetUp(files);
			Process_waybills();

			//var clientDirectory = Path.Combine(Settings.Default.FTPOptBoxPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var clientDirectory = Path.Combine(Settings.Default.DocumentPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var savedFiles = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*.*", SearchOption.AllDirectories);
			Assert.That(savedFiles.Count(), Is.EqualTo(2));

			var bodyFile = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*(b271433).dbf",
				SearchOption.AllDirectories);
			Assert.That(bodyFile.Count(), Is.EqualTo(1));

			var headerFile = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*(h271433).dbf",
				SearchOption.AllDirectories);
			Assert.That(headerFile.Count(), Is.EqualTo(1));
			using (new SessionScope())
			{
				var documents = Document.Queryable.Where(doc => doc.FirmCode == _summary.Supplier.Id &&
					doc.ClientCode == _summary.Client.Id && doc.AddressId == _summary.Client.Addresses[0].Id);
				Assert.That(documents.Count(), Is.EqualTo(1));
			}

			var tmpFiles = Directory.GetFiles(Path.Combine(Settings.Default.TempPath, "DownWAYBILL"), "*.*");
			Assert.That(tmpFiles.Count(), Is.EqualTo(0), "не удалили временный файл {0}", tmpFiles.Implode());
		}

		[Test]
		public void Parse_multifile_document_in_archive()
		{
			var files = new List<string> { @"..\..\Data\Waybills\multifile\apteka_holding.rar" };
			SetUp(files);
			Process_waybills();

			//var clientDirectory = Path.Combine(Settings.Default.FTPOptBoxPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var clientDirectory = Path.Combine(Settings.Default.DocumentPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var savedFiles = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*.*", SearchOption.AllDirectories);
			Assert.That(savedFiles.Count(), Is.EqualTo(2));
		}

		[Test, Description("В одном архиве находятся многофайловая и однофайловая накладные")]
		public void Parse_multifile_with_single_document()
		{
			var files = new List<string> { @"..\..\Data\Waybills\multifile\multifile_with_single.zip" };
			SetUp(files);
			Process_waybills();

			//var clientDirectory = Path.Combine(Settings.Default.FTPOptBoxPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var clientDirectory = Path.Combine(Settings.Default.DocumentPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var savedFiles = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*.*", SearchOption.AllDirectories);
			Assert.That(savedFiles.Count(), Is.EqualTo(3));

			var singleFile = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*(0000470553).dbf",
				SearchOption.AllDirectories);
			Assert.That(singleFile.Count(), Is.EqualTo(1));			
		}

		[Test]
		public void Parse_with_more_then_one_common_column()
		{
			var files = new List<string> {
				@"..\..\Data\Waybills\multifile\h1766399.dbf",
				@"..\..\Data\Waybills\multifile\b1766399.dbf"
			};
			SetUp(files);
			Process_waybills();

			//var clientDirectory = Path.Combine(Settings.Default.FTPOptBoxPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var clientDirectory = Path.Combine(Settings.Default.DocumentPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var savedFiles = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*.*", SearchOption.AllDirectories);
			Assert.That(savedFiles.Count(), Is.EqualTo(2));
		}

		[Test, Description("Накладная в формате dbf и первая буква в имени h (как у заголовка двухфайловой накладной)")]
		public void Parse_when_waybill_like_multifile_header()
		{
			var files = new List<string> {@"..\..\Data\Waybills\h1016416.DBF",};

			SetUp(files);
			Process_waybills();

			CheckClientDirectory(1, DocType.Waybill);
			CheckDocumentLogEntry(1);
			CheckDocumentEntry(1);
		}

		[Test, Description("Накладная в формате dbf и первая буква в имени b (как у тела двухфайловой накладной)")]
		public void Parse_when_waybill_like_multifile_body()
		{
			var files = new List<string> { @"..\..\Data\Waybills\bi055540.DBF", };

			SetUp(files);
			Process_waybills();

			CheckClientDirectory(1, DocType.Waybill);
			CheckDocumentLogEntry(1);
			CheckDocumentEntry(1);
		}

		[Test, Description("2 накладные в формате dbf, первые буквы имен h и b, но это две разные накладные")]
		public void Parse_like_multifile_but_is_not()
		{
			var files = new List<string> { @"..\..\Data\Waybills\h1016416.DBF", @"..\..\Data\Waybills\bi055540.DBF", };

			SetUp(files);
			Process_waybills();

			CheckClientDirectory(2, DocType.Waybill);
			CheckDocumentLogEntry(2);
			CheckDocumentEntry(2);
		}

		[Test]
		public void Parse_Schipakin()
		{
			var files = new List<string> { @"..\..\Data\Waybills\multifile\h160410.dbf", @"..\..\Data\Waybills\multifile\b160410.dbf" };
			SetUp(files);
			Process_waybills();

			CheckClientDirectory(2, DocType.Waybill);
			CheckDocumentLogEntry(2);
			CheckDocumentEntry(1);
		}

		[Test, Ignore("Оставляю на случай если нужно положить письмо с накладной в ящик и подебажить")]
		public void Parse_eml_file()
		{
			var files = new List<string> {
				@"C:\Users\dorofeev\Desktop\WaybillUnparse.eml",
			};
			IsEmlFile = true;
			SetUp(files);
			Process_waybills();
		}

		[Test]
		public void Check_destination_addresses()
		{
			string emailList = String.Empty;

			TestClient client = TestClient.Create();

			var handler = new WaybillSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);
			Mime message = Mime.Parse(@"..\..\Data\EmailSourceHandlerTest\WithCC.eml");

			message.MainEntity.To.Clear();
			message.MainEntity.Cc.Clear();

			GroupAddress addrTo = new GroupAddress();		
			addrTo.GroupMembers.Add(new MailboxAddress("klpuls@mail.ru"));
			message.MainEntity.To.Add(addrTo);

			GroupAddress addrCc = new GroupAddress();
			addrCc.GroupMembers.Add(new MailboxAddress(String.Format("{0}@waybills.analit.net", client.Addresses[0].Id)));
			addrCc.GroupMembers.Add(new MailboxAddress("a_andreychenkov@oryol.protek.ru"));
			message.MainEntity.Cc.Add(addrCc);

			handler.CheckMimeTest(message);
			
		}

		[Test]
		public void test_process_waybill_if_supplier_disabled()
		{
			var fileNames = new List<string> { @"..\..\Data\Waybills\0000470553.dbf" };
			SetUp(fileNames);
			var supplier = _summary.Supplier;
			supplier.Disabled = true;
			using (var transaction = new TransactionScope(OnDispose.Rollback))
			{
				supplier.Save();				
				transaction.VoteCommit();
			}		
			var handler = Process_waybills();
			Assert.That(handler.Result, Is.True);			
		}
	}
}
