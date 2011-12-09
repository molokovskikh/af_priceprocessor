using System;
using System.Collections.Generic;
using System.Linq;
using Common.MySql;
using Common.Tools;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using LumiSoft.Net.Mime;
using NUnit.Framework;
using Inforoom.Downloader;
using System.IO;
using MySql.Data.MySqlClient;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using Test.Support.Suppliers;
using Castle.ActiveRecord;

namespace PriceProcessor.Test
{
	public class SummaryInfo
	{
		public TestClient Client { get; set; }
		public TestSupplier Supplier { get; set; }
	}

	public class WaybillSourceHandlerForTesting : WaybillSourceHandler
	{		
		public WaybillSourceHandlerForTesting(string mailbox, string password)
			: base(mailbox, password)
		{
		}

		public void Process()
		{
			CreateDirectoryPath();
			ProcessData();
		}
	}

	[TestFixture]
	public class WaybillSourceHandlerFixture
	{
		private SummaryInfo _summary = new SummaryInfo();

		private bool IsEmlFile;

		[SetUp]
		public void DeleteDirectories()
		{
			TestHelper.RecreateDirectories();
			ImapHelper.ClearImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);
		}

		private static void PrepareSupplier(TestSupplier supplier, string from)
		{
			supplier.WaybillSource.SourceType = WaybillSourceType.Email;
			supplier.WaybillSource.EMailFrom = from;
			supplier.Save();
		}

		private static void PrepareClient(TestClient client)
		{
			With.Connection(c => {
				var command = new MySqlCommand(
					"UPDATE usersettings.RetClientsSet SET ParseWaybills = 1 WHERE ClientCode = ?ClientCode",
					c);
				command.Parameters.AddWithValue("?ClientCode", client.Id);
				command.ExecuteNonQuery();
			});
		}

		public void SetUp(IList<string> fileNames)
		{
			var client = TestClient.Create();
			var supplier = TestSupplier.Create();

			var from = String.Format("{0}@test.test", client.Id);
			PrepareSupplier(supplier, from);
			PrepareClient(client);

			byte[] bytes;
			if (IsEmlFile)
				bytes = File.ReadAllBytes(fileNames[0]);
			else
			{
				var message = ImapHelper.BuildMessageWithAttachments(
					String.Format("{0}@waybills.analit.net", client.Addresses[0].Id),
					from, fileNames.ToArray());
				bytes = message.ToByteData();
			}

			ImapHelper.StoreMessage(
				Settings.Default.TestIMAPUser,
				Settings.Default.TestIMAPPass,
				Settings.Default.IMAPSourceFolder, bytes);
			_summary.Client = client;
			_summary.Supplier = supplier;
		}

		private void CheckClientDirectory(int waitingFilesCount, DocType documentsType)
		{
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
					doc.Address.Id == _summary.Client.Addresses[0].Id);
				Assert.That(documents.Count(), Is.EqualTo(waitingCountEntries));
			}			
		}

		private WaybillSourceHandlerForTesting Process()
		{			
			WaybillSourceHandlerForTesting handler = new WaybillSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);
			handler.Process();
			return handler;
		}

		[Test, Description("Проверка вставки даты документа после разбора накладной")]
		public void Check_document_date()
		{
			SetUp(new List<string> {@"..\..\Data\Waybills\0000470553.dbf"});
			Process();
			using (new SessionScope())
			{
				var documents = Document.Queryable.Where(doc => doc.FirmCode == _summary.Supplier.Id &&
					doc.ClientCode == _summary.Client.Id &&
					doc.Address.Id == _summary.Client.Addresses[0].Id &&
					doc.DocumentDate != null);
				Assert.That(documents.Count(), Is.EqualTo(1));
			}
		}

		[Test, Description("Проверка, что накладная скопирована в папку клиенту")]
		public void Check_copy_waybill_to_client_dir()
		{
			var fileNames = new List<string> { @"..\..\Data\Waybills\0000470553.dbf" };
			SetUp(fileNames);
			Process();

			Assert.That(GetSavedFiles("*(0000470553).dbf").Count(), Is.EqualTo(1));

			var tempFilePath = Path.Combine(Settings.Default.TempPath, typeof(WaybillSourceHandlerForTesting).Name);
			tempFilePath = Path.Combine(tempFilePath, "0000470553.dbf");
			Assert.IsFalse(File.Exists(tempFilePath));
		}

		[Test, Description("Тест для случая когда два поставщика имеют один и тот же email в Waybill_wources (обычно это филиалы одного и того же поставщика)")]
		public void Send_waybill_from_supplier_with_filials()
		{
			var fileNames = new List<string> { @"..\..\Data\Waybills\0000470553.dbf" };
			SetUp(fileNames);

			var supplier = TestSupplier.Create(64UL);
			PrepareSupplier(supplier, String.Format("{0}@test.test", _summary.Client));
			Process();

			var savedFiles = GetSavedFiles("*(0000470553).dbf");
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
			Process();

			var clientDirectory = Path.Combine(Settings.Default.DocumentPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var savedFiles = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*.*", SearchOption.AllDirectories);
			Assert.That(savedFiles.Count(), Is.EqualTo(2));

			var bodyFile = GetSavedFiles("*(b271433).dbf");
			Assert.That(bodyFile.Count(), Is.EqualTo(1));

			var headerFile = GetSavedFiles("*(h271433).dbf");
			Assert.That(headerFile.Count(), Is.EqualTo(1));
			using (new SessionScope())
			{
				var documents = Document.Queryable.Where(doc => doc.FirmCode == _summary.Supplier.Id
					&& doc.ClientCode == _summary.Client.Id
					&& doc.Address.Id == _summary.Client.Addresses[0].Id);
				Assert.That(documents.Count(), Is.EqualTo(1));
			}

			var tmpFiles = Directory.GetFiles(Path.Combine(Settings.Default.TempPath, typeof(WaybillSourceHandlerForTesting).Name), "*.*");
			Assert.That(tmpFiles.Count(), Is.EqualTo(0), "не удалили временный файл {0}", tmpFiles.Implode());
		}

		[Test]
		public void Parse_multifile_document_in_archive()
		{
			var files = new List<string> { @"..\..\Data\Waybills\multifile\apteka_holding.rar" };
			SetUp(files);
			Process();

			var savedFiles = GetSavedFiles();
			Assert.That(savedFiles.Count(), Is.EqualTo(2));
		}

		[Test, Description("В одном архиве находятся многофайловая и однофайловая накладные")]
		public void Parse_multifile_with_single_document()
		{
			var files = new List<string> { @"..\..\Data\Waybills\multifile\multifile_with_single.zip" };
			SetUp(files);
			Process();

			var savedFiles = GetSavedFiles();
			Assert.That(savedFiles.Count(), Is.EqualTo(3));
			Assert.That(savedFiles.Where(f => f.EndsWith("(0000470553).dbf")).Count(), Is.EqualTo(1));
		}

		private string[] GetSavedFiles(string mask = "*.*")
		{
			var clientDirectory = Path.Combine(Settings.Default.DocumentPath, _summary.Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			return Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), mask, SearchOption.AllDirectories);
		}

		[Test]
		public void Parse_with_more_then_one_common_column()
		{
			var files = new List<string> {
				@"..\..\Data\Waybills\multifile\h1766399.dbf",
				@"..\..\Data\Waybills\multifile\b1766399.dbf"
			};
			SetUp(files);
			Process();

			Assert.That(GetSavedFiles().Count(), Is.EqualTo(2));
		}

		[Test, Description("Накладная в формате dbf и первая буква в имени h (как у заголовка двухфайловой накладной)")]
		public void Parse_when_waybill_like_multifile_header()
		{
			var files = new List<string> {@"..\..\Data\Waybills\h1016416.DBF",};

			SetUp(files);
			Process();

			CheckClientDirectory(1, DocType.Waybill);
			CheckDocumentLogEntry(1);
			CheckDocumentEntry(1);
		}

		[Test, Description("Накладная в формате dbf и первая буква в имени b (как у тела двухфайловой накладной)")]
		public void Parse_when_waybill_like_multifile_body()
		{
			var files = new List<string> { @"..\..\Data\Waybills\bi055540.DBF", };

			SetUp(files);
			Process();

			CheckClientDirectory(1, DocType.Waybill);
			CheckDocumentLogEntry(1);
			CheckDocumentEntry(1);
		}

		[Test, Description("2 накладные в формате dbf, первые буквы имен h и b, но это две разные накладные")]
		public void Parse_like_multifile_but_is_not()
		{
			var files = new List<string> { @"..\..\Data\Waybills\h1016416.DBF", @"..\..\Data\Waybills\bi055540.DBF", };

			SetUp(files);
			Process();

			CheckClientDirectory(2, DocType.Waybill);
			CheckDocumentLogEntry(2);
			CheckDocumentEntry(2);
		}

		[Test]
		public void Parse_Schipakin()
		{
			var files = new List<string> { @"..\..\Data\Waybills\multifile\h160410.dbf", @"..\..\Data\Waybills\multifile\b160410.dbf" };
			SetUp(files);
			Process();

			CheckClientDirectory(2, DocType.Waybill);
			CheckDocumentLogEntry(2);
			CheckDocumentEntry(1);
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

			handler.CheckMime(message);
		}

		[Test]
		public void Ignore_document_for_wrong_address()
		{
			var begin = DateTime.Now;
			var supplier = TestSupplier.Create();
			var from = String.Format("{0}@test.test", supplier.Id);
			PrepareSupplier(supplier, from);

			var message = ImapHelper.BuildMessageWithAttachments(
				String.Format("{0}@waybills.analit.net", "1"),
				from,
				new[] {@"..\..\Data\Waybills\bi055540.DBF"});
			var bytes = message.ToByteData();

			ImapHelper.StoreMessage(
				Settings.Default.TestIMAPUser,
				Settings.Default.TestIMAPPass,
				Settings.Default.IMAPSourceFolder, bytes);
			_summary.Supplier = supplier;

			Process();

			var docs = TestDocumentLog.Queryable.Where(d => d.LogTime > begin).ToList();
			Assert.That(docs.Count, Is.EqualTo(0));
		}

		[Test, Ignore("Разбор ситуации с письмом")]
		public void CheckTrouble()
		{
			//IsEmlFile = true;
			//SetUp();
			//3649142.eml
			var emailList = String.Empty;

			var client = TestClient.Create();


			///var handler = new WaybillSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);

			//handler.CheckMimeTest(message);

			var begin = DateTime.Now;
			var supplier = TestSupplier.Create();
			var from = String.Format("{0}@test.test", supplier.Id);
			PrepareSupplier(supplier, from);

			Mime message = Mime.Parse(@"..\..\Data\3649142.eml");

			message.MainEntity.To.Clear();
			message.MainEntity.To.Add(new MailboxAddress(String.Format("{0}@waybills.analit.net", client.Addresses[0].Id)));

			message.MainEntity.From.Clear();
			message.MainEntity.From.Add(new MailboxAddress(from));

			var bytes = message.ToByteData();

			//var message = TestHelper.BuildMessageWithAttachments(
			//    String.Format("{0}@waybills.analit.net", "1"),
			//    from,
			//    new[] {@"..\..\Data\Waybills\bi055540.DBF"});
			//var bytes = message.ToByteData();

			ImapHelper.StoreMessage(
				Settings.Default.TestIMAPUser,
				Settings.Default.TestIMAPPass,
				Settings.Default.IMAPSourceFolder, bytes);

			_summary.Supplier = supplier;
			_summary.Client = client;

			Process();

		}
	}
}
