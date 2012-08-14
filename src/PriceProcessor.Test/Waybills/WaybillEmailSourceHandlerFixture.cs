using System;
using System.Collections.Generic;
using System.Linq;
using Common.MySql;
using Common.Tools;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
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
using WaybillSourceType = Test.Support.WaybillSourceType;

namespace PriceProcessor.Test.Waybills
{
	public class WaybillEmailSourceHandlerForTesting : WaybillEmailSourceHandler
	{
		public List<Mime> Sended = new List<Mime>();

		public WaybillEmailSourceHandlerForTesting(string mailbox, string password)
			: base(mailbox, password)
		{
		}

		public void Process()
		{
			CreateDirectoryPath();
			ProcessData();
		}

		protected override void Send(Mime mime)
		{
			Sended.Add(mime);
		}
	}

	[TestFixture]
	public class WaybillEmailSourceHandlerFixture
	{
		public TestClient Client { get; set; }
		public TestSupplier Supplier { get; set; }

		private bool IsEmlFile;
		private WaybillEmailSourceHandlerForTesting handler;

		private EventFilter<WaybillService> filter;

		[SetUp]
		public void DeleteDirectories()
		{
			TestHelper.RecreateDirectories();
			ImapHelper.ClearImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);

			filter = new EventFilter<WaybillService>();
		}

		[TearDown]
		public void TearDown()
		{
			filter.Reset();
			var events = filter.Events
				.Where(e => e.ExceptionObject.Message != "Не удалось определить тип парсера")
				.ToArray();
			Assert.That(events, Is.Empty, filter.Events.Implode(e => e.ExceptionObject));
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
			Client = client;
			Supplier = supplier;
		}

		private void CheckClientDirectory(int waitingFilesCount, DocType documentsType)
		{
			var clientDirectory = Path.Combine(Settings.Default.DocumentPath, Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var savedFiles = Directory.GetFiles(Path.Combine(clientDirectory, documentsType + "s"), "*.*", SearchOption.AllDirectories);
			Assert.That(savedFiles.Count(), Is.EqualTo(waitingFilesCount));
		}

		private void CheckDocumentLogEntry(int waitingCountEntries)
		{
			using (new SessionScope())
			{
				var logs = TestDocumentLog.Queryable.Where(log =>
					log.Client.Id == Client.Id &&
					log.Supplier.Id == Supplier.Id &&
					log.AddressId == Client.Addresses[0].Id);
				Assert.That(logs.Count(), Is.EqualTo(waitingCountEntries));
			}
		}

		private void CheckDocumentEntry(int waitingCountEntries)
		{
			using (new SessionScope())
			{
				var documents = Document.Queryable.Where(doc => doc.FirmCode == Supplier.Id &&
					doc.ClientCode == Client.Id &&
					doc.Address.Id == Client.Addresses[0].Id);
				Assert.That(documents.Count(), Is.EqualTo(waitingCountEntries));
			}
		}

		private void Process()
		{
			handler = new WaybillEmailSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);
			handler.Process();
		}

		[Test, Description("Проверка вставки даты документа после разбора накладной")]
		public void Check_document_date()
		{
			SetUp(new List<string> {@"..\..\Data\Waybills\0000470553.dbf"});
			Process();
			using (new SessionScope())
			{
				var documents = Document.Queryable.Where(doc => doc.FirmCode == Supplier.Id &&
					doc.ClientCode == Client.Id &&
					doc.Address.Id == Client.Addresses[0].Id &&
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

			var tempFilePath = Path.Combine(Settings.Default.TempPath, typeof(WaybillEmailSourceHandlerForTesting).Name);
			tempFilePath = Path.Combine(tempFilePath, "0000470553.dbf");
			Assert.IsFalse(File.Exists(tempFilePath));
		}

		[Test, Description("Тест для случая когда два поставщика имеют один и тот же email в Waybill_wources (обычно это филиалы одного и того же поставщика)")]
		public void Send_waybill_from_supplier_with_filials()
		{
			var fileNames = new List<string> { @"..\..\Data\Waybills\0000470553.dbf" };
			SetUp(fileNames);

			var supplier = TestSupplier.Create(64UL);
			PrepareSupplier(supplier, String.Format("{0}@test.test", Client));
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

			var clientDirectory = Path.Combine(Settings.Default.DocumentPath, Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var savedFiles = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*.*", SearchOption.AllDirectories);
			Assert.That(savedFiles.Count(), Is.EqualTo(2));

			var bodyFile = GetSavedFiles("*(b271433).dbf");
			Assert.That(bodyFile.Count(), Is.EqualTo(1));

			var headerFile = GetSavedFiles("*(h271433).dbf");
			Assert.That(headerFile.Count(), Is.EqualTo(1));
			using (new SessionScope())
			{
				var documents = Document.Queryable.Where(doc => doc.FirmCode == Supplier.Id
					&& doc.ClientCode == Client.Id
					&& doc.Address.Id == Client.Addresses[0].Id);
				Assert.That(documents.Count(), Is.EqualTo(1));
			}

			var tmpFiles = Directory.GetFiles(Path.Combine(Settings.Default.TempPath, typeof(WaybillEmailSourceHandlerForTesting).Name), "*.*");
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
			var clientDirectory = Path.Combine(Settings.Default.DocumentPath, Client.Addresses[0].Id.ToString().PadLeft(3, '0'));
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
			var client = TestClient.Create();

			var handler = new WaybillEmailSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);
			var message = Mime.Parse(@"..\..\Data\EmailSourceHandlerTest\WithCC.eml");

			message.MainEntity.To.Clear();
			message.MainEntity.Cc.Clear();

			var addrTo = new GroupAddress();
			addrTo.GroupMembers.Add(new MailboxAddress("klpuls@mail.ru"));
			message.MainEntity.To.Add(addrTo);

			var addrCc = new GroupAddress();
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
			Supplier = supplier;

			Process();

			var docs = TestDocumentLog.Queryable.Where(d => d.LogTime > begin).ToList();
			Assert.That(docs.Count, Is.EqualTo(0));
		}

		[Test]
		public void Reject_message_for_client_with_another_region()
		{
			Client = TestClient.Create(2, 2);
			Supplier = TestSupplier.Create();
			Supplier.WaybillSource.SourceType = WaybillSourceType.Email;
			Supplier.WaybillSource.EMailFrom = String.Format("{0}@sup.com", Supplier.Id);
			Supplier.Save();

			handler = new WaybillEmailSourceHandlerForTesting("", "");
			handler.CreateDirectoryPath();

			var mime = new Mime();
			mime.MainEntity.Subject = "Тестовое сообщение";
			mime.MainEntity.To = new AddressList {
				new MailboxAddress(String.Format("{0}@waybills.analit.net", Client.Addresses[0].Id))
			};
			mime.MainEntity.From = new AddressList {
				new MailboxAddress(String.Format("{0}@sup.com", Supplier.Id))
			};
			mime.MainEntity.ContentType = MediaType_enum.Multipart_mixed;
			mime.MainEntity.ChildEntities.Add(new MimeEntity {
				ContentDisposition = ContentDisposition_enum.Attachment,
				ContentType = MediaType_enum.Text_plain,
				ContentTransferEncoding = ContentTransferEncoding_enum.Base64,
				ContentDisposition_FileName = "text.txt",
				Data = Enumerable.Repeat(100, 100).Select(i => (byte)i).ToArray()
			});
			handler.ProcessMime(mime);

			Assert.That(handler.Sended.Count, Is.EqualTo(1));
			var message = handler.Sended[0];
			Assert.That(message.MainEntity.Subject, Is.EqualTo("Ваше Сообщение не доставлено одной или нескольким аптекам"));
			Assert.That(message.MainEntity.ChildEntities[0].DataText, Is.StringContaining("с темой: \"Тестовое сообщение\" не были доставлены аптеке, т.к. указанный адрес получателя"));
		}
	}
}
