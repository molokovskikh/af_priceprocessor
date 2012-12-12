﻿using System;
using System.Collections.Generic;
using System.Data;
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
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using Test.Support.Suppliers;
using Castle.ActiveRecord;
using FileHelper = Common.Tools.FileHelper;
using WaybillSourceType = Test.Support.WaybillSourceType;

namespace PriceProcessor.Test.Waybills.Handlers
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
	public class WaybillEmailSourceHandlerFixture : BaseWaybillHandlerFixture
	{
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

		public void SetUp(IList<string> fileNames)
		{
			client = TestClient.Create();
			supplier = TestSupplier.Create();

			var from = String.Format("{0}@test.test", client.Id);
			PrepareSupplier(supplier, from);
			PrepareClient(client);

			byte[] bytes;
			if (IsEmlFile)
				bytes = File.ReadAllBytes(fileNames[0]);
			else {
				var message = ImapHelper.BuildMessageWithAttachments(
					String.Format("{0}@waybills.analit.net", client.Addresses[0].Id),
					from, fileNames.ToArray());
				bytes = message.ToByteData();
			}

			ImapHelper.StoreMessage(
				Settings.Default.TestIMAPUser,
				Settings.Default.TestIMAPPass,
				Settings.Default.IMAPSourceFolder, bytes);
		}


		[Test, Description("Проверка вставки даты документа после разбора накладной")]
		public void Check_document_date()
		{
			SetUp(new List<string> { @"..\..\Data\Waybills\0000470553.dbf" });
			Process();
			using (new SessionScope()) {
				var documents = Document.Queryable.Where(doc => doc.FirmCode == supplier.Id &&
					doc.ClientCode == client.Id &&
					doc.Address.Id == client.Addresses[0].Id &&
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
			PrepareSupplier(supplier, String.Format("{0}@test.test", client));
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

			var clientDirectory = Path.Combine(Settings.Default.DocumentPath, client.Addresses[0].Id.ToString().PadLeft(3, '0'));
			var savedFiles = Directory.GetFiles(Path.Combine(clientDirectory, "Waybills"), "*.*", SearchOption.AllDirectories);
			Assert.That(savedFiles.Count(), Is.EqualTo(2));

			var bodyFile = GetSavedFiles("*(b271433).dbf");
			Assert.That(bodyFile.Count(), Is.EqualTo(1));

			var headerFile = GetSavedFiles("*(h271433).dbf");
			Assert.That(headerFile.Count(), Is.EqualTo(1));
			using (new SessionScope()) {
				var documents = Document.Queryable.Where(doc => doc.FirmCode == supplier.Id
					&& doc.ClientCode == client.Id
					&& doc.Address.Id == client.Addresses[0].Id);
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
			var clientDirectory = Path.Combine(Settings.Default.DocumentPath, client.Addresses[0].Id.ToString().PadLeft(3, '0'));
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
			var files = new List<string> { @"..\..\Data\Waybills\h1016416.DBF", };

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
		public void Parse_if_one_waydill_exclude()
		{
			var files = new List<string> { @"..\..\Data\Waybills\h1016416.DBF", @"..\..\Data\Waybills\bi055540.DBF", };
			SetUp(files);

			With.Connection(c => {
				var helper = new MySqlHelper(c);
				helper.Command(string.Format("insert into usersettings.WaybillExcludeFile (Mask, Supplier) value ('h1016', {0});", supplier.Id)).Execute();
			});

			Process();

			CheckClientDirectory(2, DocType.Waybill);
			var logs = CheckDocumentLogEntry(2);
			CheckDocumentEntry(1);

			Assert.IsTrue(logs.Any(l => l.Addition == "Файл накладной не принят к обработке по причине запрета маски h1016 у поставщика"));

			With.Connection(c => {
				var helper = new MySqlHelper(c);
				var ds = ((CommandHelper)helper.Command("select * from usersettings.waybilldirtyfile")).Fill();
				Assert.AreEqual(ds.Tables[0].Rows.Count, 1);
				foreach (DataRow row in ds.Tables[0].Rows) {
					Assert.AreEqual(row["Supplier"].ToString(), supplier.Id.ToString());
					Assert.AreEqual(row["Mask"].ToString(), "h1016");
					Assert.That(row["File"].ToString(), Is.StringContaining("h1016416.DBF"));
				}
				helper.Command("delete from usersettings.WaybillExcludeFile; delete from usersettings.waybilldirtyfile;").Execute();
			});
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
				new[] { @"..\..\Data\Waybills\bi055540.DBF" });
			var bytes = message.ToByteData();

			ImapHelper.StoreMessage(
				Settings.Default.TestIMAPUser,
				Settings.Default.TestIMAPPass,
				Settings.Default.IMAPSourceFolder, bytes);
			this.supplier = supplier;

			Process();

			var docs = TestDocumentLog.Queryable.Where(d => d.LogTime > begin).ToList();
			Assert.That(docs.Count, Is.EqualTo(0));
		}

		[Test]
		public void Reject_message_for_client_with_another_region()
		{
			client = TestClient.Create(2, 2);
			supplier = TestSupplier.Create();
			supplier.WaybillSource.SourceType = WaybillSourceType.Email;
			supplier.WaybillSource.EMailFrom = String.Format("{0}@sup.com", supplier.Id);
			supplier.Save();

			handler = new WaybillEmailSourceHandlerForTesting("", "");
			handler.CreateDirectoryPath();

			var mime = new Mime();
			mime.MainEntity.Subject = "Тестовое сообщение";
			mime.MainEntity.To = new AddressList {
				new MailboxAddress(String.Format("{0}@waybills.analit.net", client.Addresses[0].Id))
			};
			mime.MainEntity.From = new AddressList {
				new MailboxAddress(String.Format("{0}@sup.com", supplier.Id))
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

		[Test]
		public void Process_message_if_from_contains_more_than_one_address()
		{
			client = TestClient.Create();
			address = client.Addresses[0];
			supplier = TestSupplier.Create();

			supplier.WaybillSource.EMailFrom = String.Format("edata{0}@msk.katren.ru", supplier.Id);
			supplier.WaybillSource.SourceType = WaybillSourceType.Email;
			supplier.Save();

			FileHelper.DeleteDir(Settings.Default.DocumentPath);

			ImapHelper.ClearImapFolder();
			var mime = PatchTo(@"..\..\Data\Unparse.eml",
				String.Format("{0}@waybills.analit.net", address.Id),
				String.Format("edata{0}@msk.katren.ru,vbskript@katren.ru", supplier.Id));
			ImapHelper.StoreMessage(mime.ToByteData());

			Process();

			var files = GetFileForAddress(DocType.Waybill);
			Assert.That(files.Length, Is.EqualTo(1), "не обработали документ");
		}

		[Test]
		public void Parse_waybill_if_parsing_enabled()
		{
			client = TestClient.Create();
			supplier = TestSupplier.Create();

			var beign = DateTime.Now;
			//Удаляем миллисекунды из даты, т.к. они не сохраняются в базе данных
			beign = beign.AddMilliseconds(-beign.Millisecond);

			var email = String.Format("edata{0}@msk.katren.ru", supplier.Id);
			supplier.WaybillSource.EMailFrom = email;
			supplier.WaybillSource.SourceType = WaybillSourceType.Email;
			supplier.Save();
			client.Settings.ParseWaybills = true;
			client.Save();

			ImapHelper.ClearImapFolder();
			ImapHelper.StoreMessageWithAttachToImapFolder(
				String.Format("{0}@waybills.analit.net", client.Addresses[0].Id),
				email,
				@"..\..\Data\Waybills\8916.dbf");

			Process();

			var files = GetFileForAddress(DocType.Waybill);
			Assert.That(files.Length, Is.EqualTo(1));

			using (new SessionScope()) {
				var logs = TestDocumentLog.Queryable.Where(d => d.Client.Id == client.Id).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));
				var log = logs.Single();
				var logTime = log.LogTime;
				Assert.That(logTime.Date.AddHours(logTime.Hour).AddMinutes(logTime.Minute).AddSeconds(logTime.Second),
					Is.GreaterThanOrEqualTo(beign.Date.AddHours(beign.Hour).AddMinutes(beign.Minute).AddSeconds(beign.Second)));
				Assert.That(log.DocumentSize, Is.GreaterThan(0));

				var documents = Document.Queryable.Where(d => d.Log.Id == log.Id).ToList();
				Assert.That(documents.Count, Is.EqualTo(1));
				Assert.That(documents.Single().Lines.Count, Is.EqualTo(7));
			}
		}

		private static void PrepareSupplier(TestSupplier supplier, string from)
		{
			supplier.WaybillSource.SourceType = WaybillSourceType.Email;
			supplier.WaybillSource.EMailFrom = from;
			supplier.Save();
		}

		private void Process()
		{
			handler = new WaybillEmailSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);
			handler.Process();
		}

		private Mime PatchTo(string file, string to, string from)
		{
			var mime = Mime.Parse(file);
			var main = mime.MainEntity;
			main.To.Clear();
			main.To.Parse(to);
			main.From.Clear();
			main.From.Parse(from);
			return mime;
		}
	}
}