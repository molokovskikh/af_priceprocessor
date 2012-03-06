using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Castle.ActiveRecord;
using Inforoom.Downloader;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using LumiSoft.Net.Mime;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using Test.Support.Logs;
using Test.Support.Suppliers;
using Common.Tools;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Filter;

namespace PriceProcessor.Test.Waybills
{
	public class DocSourceHandlerTestInfo
	{
		public TestSupplier Supplier { get; set; }
		public TestRegion Region { get; set; }
		public IList<TestUser> Users { get; set; }
		public Mime Mime { get; set;}
	}

	public class DocSourceHandlerForTesting : DocSourceHandler
	{
		public DocSourceHandlerForTesting(string mailbox, string password)
			: base(mailbox, password)
		{
		}

		public void TestProcessMime(Mime m)
		{
			CreateDirectoryPath();
			ProcessMime(m);
		}
	}

	[TestFixture]
	public class DocSourceHandlerFixture
	{
		private DocSourceHandlerTestInfo _info;

		[SetUp]
		public void DeleteDirectories()
		{
			SetDefaultValues();
			_info = null;
			TestHelper.RecreateDirectories();
			ImapHelper.ClearImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);
		}

		private void SetDefaultValues()
		{
			using (new TransactionScope()) {
				var values = TemplateHolder.Values;

				values.AllowedMiniMailExtensions = "doc, xls, gif, tiff, tif, jpg, pdf, txt";

				values.ResponseSubjectMiniMailOnUnknownProvider = "Ваше Сообщение не доставлено одной или нескольким аптекам";
				values.ResponseSubjectMiniMailOnEmptyRecipients = "Ваше Сообщение не доставлено одной или нескольким аптекам";
				values.ResponseSubjectMiniMailOnMaxAttachment = "Ваше Сообщение не доставлено одной или нескольким аптекам";
				values.ResponseSubjectMiniMailOnAllowedExtensions = "Ваше Сообщение не доставлено одной или нескольким аптекам";
				values.ResponseSubjectMiniMailOnEmptyLetter = "Ваше Сообщение не доставлено одной или нескольким аптекам";

				values.ResponseBodyMiniMailOnUnknownProvider = "Здравствуйте! Ваше письмо с темой {0} неизвестный адрес {1} С уважением";
				values.ResponseBodyMiniMailOnEmptyRecipients = "Здравствуйте! Ваше письмо с темой {0} не будет доставлено по причинам {1} С уважением";
				values.ResponseBodyMiniMailOnMaxAttachment = "Здравствуйте! Ваше письмо с темой {0} имеет размер {1} а должно не более {2} С уважением";
				values.ResponseBodyMiniMailOnAllowedExtensions = "Здравствуйте! Ваше письмо с темой {0} имеет расширение {1} а должно {2} С уважением";
				values.ResponseBodyMiniMailOnEmptyLetter = "Здравствуйте! Ваше письмо не содержит тему, тело и вложения С уважением";
				values.Save();
			}
		}

		private void PrepareSupplier(TestSupplier supplier, string from)
		{
			using (new TransactionScope()) {
				var group = supplier.ContactGroupOwner.AddContactGroup(ContactGroupType.MiniMails);
				group.AddContact(ContactType.Email, from);
				group.Save();
				supplier.Save();
			}
		}

		private void SetUp(IList<TestUser> users, TestRegion region, string subject, string body, IList<string> fileNames)
		{
			var info = new DocSourceHandlerTestInfo();
			var supplier = TestSupplier.Create();

			var from = String.Format("{0}@supplier.test", supplier.Id);
			PrepareSupplier(supplier, from);
			info.Supplier = supplier;

			var toList = users.Select(u => "{0}@docs.analit.net".Format(u.AvaliableAddresses[0].Id)).ToList();
			if (region != null)
				toList.Add(region.ShortAliase + "@docs.analit.net");

			var message = ImapHelper.BuildMessageWithAttachments(
				subject,
				body,
				toList.ToArray(),
				new []{from}, 
				fileNames != null ? fileNames.ToArray() : null);

			info.Mime = message;

			info.Region = region;
			info.Users = users;

			_info = info;
		}

		private void Process()
		{			
			Assert.That(_info, Is.Not.Null, "Перед обработкой должен быть вызван метод SetUp");
			Assert.That(_info.Mime, Is.Not.Null, "Перед обработкой должен быть вызван метод SetUp");
			var handler = new DocSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);
			handler.TestProcessMime(_info.Mime);
			var existsMessages = ImapHelper.CheckImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);
			Assert.That(existsMessages.Count, Is.EqualTo(0), "Существуют письма в IMAP-папками с темами: {0}", existsMessages.Select(m => m.Envelope.Subject).Implode());
		}

		[Test(Description = "Простой разбор письма")]
		public void SimpleParseMails()
		{
			var client = TestClient.Create();
			var user = client.Users[0];
			
			SetUp(
				new List<TestUser> {user},
				null,
				"Это письмо пользователю",
				"Это текст письма пользователю",
				null);

			Process();

			using (new SessionScope()) {
				var mails = TestMailSendLog.Queryable.Where(l => l.User.Id == user.Id).ToList();
				Assert.That(mails.Count, Is.EqualTo(1));

				var mailLog = mails[0];
				Assert.That(mailLog.UpdateLogEntry, Is.Null);
				Assert.That(mailLog.Committed, Is.False);
				Assert.That(mailLog.Mail.Supplier.Id, Is.EqualTo(_info.Supplier.Id));
				Assert.IsNotNullOrEmpty(mailLog.Mail.SupplierEmail);
				Assert.That(mailLog.Mail.SupplierEmail, Is.EqualTo("{0}@supplier.test".Format(_info.Supplier.Id) ));
				Assert.That(mailLog.Mail.MailRecipients.Count, Is.GreaterThan(0));
				Assert.That(mailLog.Mail.Subject, Is.EqualTo("Это письмо пользователю"));
				Assert.That(mailLog.Mail.Body, Is.EqualTo("Это текст письма пользователю"));
				Assert.That(mailLog.Mail.IsVIPMail, Is.False);
				Assert.That(mailLog.Mail.Attachments.Count, Is.EqualTo(0));
				Assert.IsNotNullOrEmpty(mailLog.Mail.SHA256Hash);
			}
		}

		[Test(Description = "разбор письма с вложениями")]
		public void ParseMailsWithAttachs()
		{
			var client = TestClient.Create();
			var user = client.Users[0];
			
			SetUp(
				new List<TestUser> {user},
				null,
				"Это письмо пользователю",
				"Это текст письма пользователю",
				new List<string> {@"..\..\Data\Waybills\moron.txt"});

			Process();

			using (new SessionScope()) {
				var mails = TestMailSendLog.Queryable.Where(l => l.User.Id == user.Id).ToList();
				Assert.That(mails.Count, Is.EqualTo(1));

				var mailLog = mails[0];
				Assert.That(mailLog.UpdateLogEntry, Is.Null);
				Assert.That(mailLog.Committed, Is.False);
				var mail = mailLog.Mail;
				Assert.That(mailLog.Recipient, Is.EqualTo(mail.MailRecipients[0]));
				Assert.That(mail.Supplier.Id, Is.EqualTo(_info.Supplier.Id));
				Assert.IsNotNullOrEmpty(mail.SupplierEmail);
				Assert.That(mail.MailRecipients.Count, Is.GreaterThan(0));
				Assert.That(mail.Subject, Is.EqualTo("Это письмо пользователю"));
				Assert.That(mail.Body, Is.EqualTo("Это текст письма пользователю"));
				Assert.That(mail.IsVIPMail, Is.False);
				Assert.That(mail.Attachments.Count, Is.EqualTo(1));
				Assert.That(mail.Size, Is.EqualTo(2453));
				Assert.IsNotNullOrEmpty(mailLog.Mail.SHA256Hash);

				var attachment = mail.Attachments[0];
				Assert.That(attachment.FileName, Is.EqualTo("moron.txt"));
				Assert.That(attachment.Extension, Is.EqualTo(".txt"));
				Assert.That(attachment.Size, Is.EqualTo(new FileInfo(@"..\..\Data\Waybills\moron.txt").Length));

				var attachLogs = TestAttachmentSendLog.Queryable.Where(l => l.User.Id == user.Id).ToList();
				Assert.That(attachLogs.Count, Is.EqualTo(1));

				var attachLog = attachLogs[0];
				Assert.That(attachLog.UpdateLogEntry, Is.Null);
				Assert.That(attachLog.Committed, Is.False);
				Assert.That(attachLog.Attachment.Id, Is.EqualTo(attachment.Id));

				Assert.That(File.Exists(Path.Combine(Settings.Default.AttachmentPath, attachment.GetSaveFileName())), Is.True);
			}
		}

		[Test]
		public void AllowExtensions()
		{
			//DefaultValues
			var values = new DefaultValues {
				AllowedMiniMailExtensions = "doc, xls, gif, tiff, tif, jpg, pdf, txt"
			};

			Assert.That(values.ExtensionAllow("doc"), Is.True);
			Assert.That(values.ExtensionAllow("txt"), Is.True);
			Assert.That(values.ExtensionAllow(".doc"), Is.True);
			Assert.That(values.ExtensionAllow(".txt"), Is.True);

			Assert.That(values.ExtensionAllow(null), Is.False);
			Assert.That(values.ExtensionAllow(""), Is.False);
			Assert.That(values.ExtensionAllow(" "), Is.False);
			Assert.That(values.ExtensionAllow("exe"), Is.False);
			Assert.That(values.ExtensionAllow(".exe"), Is.False);
		}

		static string HashToString(byte[] data)
		{
			// Create a new Stringbuilder to collect the bytes
			// and create a string.
			StringBuilder sBuilder = new StringBuilder();

			// Loop through each byte of the hashed data 
			// and format each one as a hexadecimal string.
			for (int i = 0; i < data.Length; i++)
			{
				sBuilder.Append(data[i].ToString("x2"));
			}

			// Return the hexadecimal string.
			return sBuilder.ToString();
		}

		static string GetHash(SHA256Managed sha256Hash, string input)
		{

			// Convert the input string to a byte array and compute the hash.
			byte[] data = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

			return Convert.ToBase64String(data);
		}

		// Verify a hash against a string.
		static bool VerifyHash(SHA256Managed sha256Hash, string input, string hash)
		{
			// Hash the input.
			var hashOfInput = GetHash(sha256Hash, input);

			return hashOfInput.Equals(hash, StringComparison.OrdinalIgnoreCase);
		}

		[Test(Description = "проверяем работу класса по вычислению hash SHA256")]
		public void CheckHashCompute()
		{
			var source = "Hello World!";
			var doubleSource = "Mama said goodbye!";

			using (var sha256Hash = new SHA256Managed())
			{
				var hash = GetHash(sha256Hash, source);

				Assert.IsTrue(VerifyHash(sha256Hash, source, hash));

				var firstHash = GetHash(sha256Hash, source);
				var secondHash = GetHash(sha256Hash, doubleSource);

				var finalHash = Convert.ToBase64String(sha256Hash.Hash);

				Assert.That(firstHash, Is.Not.EqualTo(secondHash));

				Assert.That(finalHash, Is.EqualTo(secondHash), "В свойстве Hash должен содержаться хеш данных, относительно которых был последний раз вызван метод ComputeHash");
			}
		}

		[Test(Description = "попытка вычислить хеш для буферов из MultiBufferStream")]
		public void ComputeHashOnMultiBufferStream()
		{
			var firstArray = Encoding.ASCII.GetBytes("this is good");
			var secondArray = Encoding.ASCII.GetBytes("this is bad");

			using (var stream = new MultiBufferStream()) {
				stream.AddBuffer(firstArray);
				stream.AddBuffer(secondArray);

				using (var sha256Hash = new SHA256Managed())
				{
					var hash = sha256Hash.ComputeHash(stream);

					var hashString = HashToString(hash);

					//Хеш для строки 'this is goodthis is bad', вычисленный с помощью внешней программы
					var verifyHash = "B61A0664186896AF7D947E6A56DEB8D608FDF092E515DB531834FDE7DBFCAF79";
					Assert.That(hashString, Is.EqualTo(verifyHash).IgnoreCase);

					hashString = Convert.ToBase64String(hash);

					var allStringHash = Convert.ToBase64String(sha256Hash.ComputeHash(Encoding.ASCII.GetBytes("this is goodthis is bad")));
					Assert.That(hashString, Is.EqualTo(allStringHash).IgnoreCase);
				}
			}
		}

		[Test(Description = "отправляем письмо со статусом VIP")]
		public void SendVIPMail()
		{
			var client = TestClient.Create();
			var user = client.Users[0];
			
			SetUp(
				new List<TestUser> {user},
				null,
				"Это письмо пользователю",
				"Это текст письма пользователю",
				null);

			var handler = new DocSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);
			handler.VIPMailPayerId = _info.Supplier.Payer.Id;
			handler.TestProcessMime(_info.Mime);

			using (new SessionScope()) {
				var mails = TestMailSendLog.Queryable.Where(l => l.User.Id == user.Id).ToList();
				Assert.That(mails.Count, Is.EqualTo(1));

				var mailLog = mails[0];
				Assert.That(mailLog.UpdateLogEntry, Is.Null);
				Assert.That(mailLog.Committed, Is.False);
				Assert.That(mailLog.Mail.Supplier.Id, Is.EqualTo(_info.Supplier.Id));
				Assert.That(mailLog.Mail.IsVIPMail, Is.True);
			}
		}

		private void SendErrorToProvider(DocSourceHandlerForTesting handler, MiniMailException exception, Mime sourceLetter)
		{
			try {

				var memoryAppender = new MemoryAppender();
				memoryAppender.AddFilter(new LoggerMatchFilter { AcceptOnMatch = true, LoggerToMatch = "PriceProcessor", Next = new DenyAllFilter() });
				BasicConfigurator.Configure(memoryAppender);

				handler.SendErrorLetterToSupplier(exception, sourceLetter);

				var events = memoryAppender.GetEvents();
				Assert.That(
					events.Length, 
					Is.EqualTo(0), 
					"Ошибки при обработки задач сертификатов:\r\n{0}", 
						events.Select(item => {
							if (string.IsNullOrEmpty(item.GetExceptionString()))
								return item.RenderedMessage;
							else
								return item.RenderedMessage + Environment.NewLine + item.GetExceptionString();
						}).Implode("\r\n"));

			}
			finally {
				LogManager.ResetConfiguration();
			}
		}

		[Test(Description = "при проверке письма должно возникнуть исключение по шаблону 'Шаблон для неизвестного адреса поставщика'")]
		public void NotFoundSupplierError()
		{
			var supplier = TestSupplier.Create();
			var from = String.Format("{0}@supplier.test", supplier.Id);
			
			var message = ImapHelper.BuildMessageWithAttachments(
				"test NotFoundSupplier",
				"body NotFoundSupplier",
				new string[] {"testUser@docs.analit.net"},
				new []{from}, 
				null);

			var handler = new DocSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);

			try {
				handler.CheckMime(message);
				Assert.Fail("Должно было возникнуть исключение MiniMailOnUnknownProviderException");
			}
			catch (MiniMailOnUnknownProviderException exception) {
				Assert.That(exception.Template, Is.EqualTo(ResponseTemplate.MiniMailOnUnknownProvider));
				Assert.That(exception.SuppliersEmails, Is.StringContaining(from));
				SendErrorToProvider(handler, exception, message);
			}
		}

		[Test(Description = "при проверке письма должно возникнуть исключение по шаблону 'Шаблон для пустого списка получателей'")]
		public void NotFoundError()
		{
			var supplier = TestSupplier.Create();
			var from = String.Format("{0}@supplier.test", supplier.Id);
			PrepareSupplier(supplier, from);

			var message = ImapHelper.BuildMessageWithAttachments(
				"test NotFound",
				"body NotFound",
				new string[] {"testUser@docs.analit.net"},
				new []{from}, 
				null);

			var handler = new DocSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);

			try {
				handler.CheckMime(message);
				Assert.Fail("Должно было возникнуть исключение MiniMailOnEmptyRecipientsException");
			}
			catch (MiniMailOnEmptyRecipientsException exception) {
				Assert.That(exception.Template, Is.EqualTo(ResponseTemplate.MiniMailOnEmptyRecipients));
				Assert.That(exception.CauseList, Is.EqualTo("testUser@docs.analit.net : " + RecipientStatus.NotFound.GetDescription()));
				SendErrorToProvider(handler, exception, message);
			}
		}

		[Test(Description = "при проверке письма должно возникнуть исключение по шаблону 'Шаблон при недопустимом типе файла вложения'")]
		public void ErrorOnAllowedExtensions()
		{
			var client = TestClient.Create();
			var user = client.Users[0];
			
			var supplier = TestSupplier.Create();
			var from = String.Format("{0}@supplier.test", supplier.Id);
			PrepareSupplier(supplier, from);

			var message = ImapHelper.BuildMessageWithAttachments(
				"test AllowedExtensions",
				"body AllowedExtensions",
				new string[]{"{0}@docs.analit.net".Format(user.AvaliableAddresses[0].Id)},
				new []{from}, 
				new string[]{@"..\..\Data\Waybills\70983_906384.zip"});

			var handler = new DocSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);

			try {
				handler.CheckMime(message);
				Assert.Fail("Должно было возникнуть исключение MiniMailOnAllowedExtensionsException");
			}
			catch (MiniMailOnAllowedExtensionsException exception) {
				Assert.That(exception.Template, Is.EqualTo(ResponseTemplate.MiniMailOnAllowedExtensions));
				Assert.That(exception.ErrorExtention, Is.EqualTo(".zip").IgnoreCase);
				Assert.That(exception.AllowedExtensions, Is.EqualTo("doc, xls, gif, tiff, tif, jpg, pdf, txt").IgnoreCase);
				SendErrorToProvider(handler, exception, message);
			}
		}

		[Test(Description = "при проверке письма должно возникнуть исключение по шаблону 'Шаблон при превышении размера вложения'")]
		public void ErrorOnMaxAttachment()
		{
			var client = TestClient.Create();
			var user = client.Users[0];
			
			var supplier = TestSupplier.Create();
			var from = String.Format("{0}@supplier.test", supplier.Id);
			PrepareSupplier(supplier, from);

			var message = ImapHelper.BuildMessageWithAttachments(
				"test MaxAttachment",
				"body MaxAttachment",
				new string[]{"{0}@docs.analit.net".Format(user.AvaliableAddresses[0].Id)},
				new []{from}, 
				new string[]{@"..\..\Data\BigMiniMailAttachment.xls"});

			var handler = new DocSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);

			try {
				handler.CheckMime(message);
				Assert.Fail("Должно было возникнуть исключение MiniMailOnMaxMailSizeException");
			}
			catch (MiniMailOnMaxMailSizeException exception) {
				Assert.That(exception.Template, Is.EqualTo(ResponseTemplate.MiniMailOnMaxAttachment));
				SendErrorToProvider(handler, exception, message);
			}
		}

		[Test(Description = "отправляем письмо два раза, второй раз оно не должно доставляться")]
		public void SendDuplicateMessage()
		{
			var client = TestClient.Create();
			var user = client.Users[0];
			
			SetUp(
				new List<TestUser> {user},
				null,
				"Это письмо пользователю",
				"Это текст письма пользователю",
				null);

			//Обрабатываем письмо один раз
			Process();

			TestMailSendLog firstLog;
			using (new SessionScope()) {
				var mails = TestMailSendLog.Queryable.Where(l => l.User.Id == user.Id).ToList();
				Assert.That(mails.Count, Is.EqualTo(1));

				var mailLog = mails[0];
				Assert.That(mailLog.Mail.Supplier.Id, Is.EqualTo(_info.Supplier.Id));
				firstLog = mailLog;
			}

			//Обрабатываем письмо повторно
			Process();

			using (new SessionScope()) {
				var mails = TestMailSendLog.Queryable.Where(l => l.User.Id == user.Id).ToList();
				Assert.That(mails.Count, Is.EqualTo(1));

				var mailLog = mails[0];
				Assert.That(mailLog.Mail.Supplier.Id, Is.EqualTo(_info.Supplier.Id));

				Assert.That(mailLog.Id, Is.EqualTo(firstLog.Id));
			}
		}

		[Test(Description = "письмо обработывается, но не по всем адресам, т.к. указывается недоступный для поставщика регион")]
		public void SendWithExclusion()
		{
			var client = TestClient.Create();
			var user = client.Users[0];
			
			var inforoomRegion = TestRegion.Find(TestRegion.Inforoom);

			SetUp(
				new List<TestUser> {user},
				inforoomRegion,
				"Это письмо пользователю",
				"Это текст письма пользователю",
				null);

			var handler = new DocSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);
			handler.TestProcessMime(_info.Mime);
			var existsMessages = ImapHelper.CheckImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);
			Assert.That(existsMessages.Count, Is.EqualTo(1), "Существуют письма в IMAP-папками с темами: {0}", existsMessages.Select(m => m.Envelope.Subject).Implode());
			Assert.That(existsMessages[0].Envelope.Subject, Is.EqualTo("Ваше Сообщение не доставлено одной или нескольким аптекам").IgnoreCase);
			
			using (new SessionScope()) {
				var mails = TestMailSendLog.Queryable.Where(l => l.User.Id == user.Id).ToList();
				Assert.That(mails.Count, Is.EqualTo(1));

				var mailLog = mails[0];
				Assert.That(mailLog.Mail.Supplier.Id, Is.EqualTo(_info.Supplier.Id));
			}
		}

		[Test(Description = "при проверке письма должно возникнуть исключение по шаблону 'Шаблон при превышении размера вложения'")]
		public void ErrorOnMaxSizeLetter()
		{
			var client = TestClient.Create();
			var user = client.Users[0];
			
			var supplier = TestSupplier.Create();
			var from = String.Format("{0}@supplier.test", supplier.Id);
			PrepareSupplier(supplier, from);

			var message = ImapHelper.BuildMessageWithAttachments(
				"test MaxSizeLetter",
				"body MaxSizeLetter",
				new string[]{"{0}@docs.analit.net".Format(user.AvaliableAddresses[0].Id)},
				new []{from}, 
				new string[]{@"..\..\Data\688.txt", @"..\..\Data\138.txt"});

			var handler = new DocSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);

			try {
				handler.CheckMime(message);
				Assert.Fail("Должно было возникнуть исключение MiniMailOnMaxMailSizeException");
			}
			catch (MiniMailOnMaxMailSizeException exception) {
				Assert.That(exception.Template, Is.EqualTo(ResponseTemplate.MiniMailOnMaxAttachment));
				SendErrorToProvider(handler, exception, message);
			}
		}

		[Test(Description = "проверяем работу метода GetSHA256Hash")]
		public void CheckGetSHA256Hash()
		{
			//пустое письмо
			var mime = new Mime();
			var hash = mime.GetSHA256Hash();
			Assert.IsEmpty(hash);

			//установлена тема письма
			mime.MainEntity.Subject = "test subject";
			hash = mime.GetSHA256Hash();
			Assert.IsNotEmpty(hash);

			//установлено тело письма как текст
			mime = ImapHelper.BuildMessageWithAttachments("", "test body", new string[]{"test@test.te"}, new string[]{"test@test.te"}, null);
			hash = mime.GetSHA256Hash();
			Assert.IsNotEmpty(hash);

			//установлено тело письма как html
			mime = ImapHelper.BuildMessageWithAttachments("", "test body", new string[]{"test@test.te"}, new string[]{"test@test.te"}, null);
			var hmtlEntity = mime.MainEntity.ChildEntities[mime.MainEntity.ChildEntities.Count-1];
			hmtlEntity.DataText = null;
			hmtlEntity.ContentType = MediaType_enum.Text_html;
			hmtlEntity.DataText = "test body html";
			hash = mime.GetSHA256Hash();
			Assert.IsNotEmpty(hash);

			//установлено все как строки с пробелами
			mime = ImapHelper.BuildMessageWithAttachments("", "test body", new string[]{"test@test.te"}, new string[]{"test@test.te"}, null);
			mime.MainEntity.Subject = "    ";
			mime.MainEntity.ChildEntities[mime.MainEntity.ChildEntities.Count-1].DataText = "    ";
			hash = mime.GetSHA256Hash();
			Assert.IsEmpty(hash);
		}

		[Test(Description = "при проверке письма должно возникнуть исключение по шаблону 'Шаблон при пустом письме'")]
		public void ErrorOnEmptyLetter()
		{
			var client = TestClient.Create();
			var user = client.Users[0];
			
			var supplier = TestSupplier.Create();
			var from = String.Format("{0}@supplier.test", supplier.Id);
			PrepareSupplier(supplier, from);

			var message = ImapHelper.BuildMessageWithAttachments(
				"  ",
				"   ",
				new string[]{"{0}@docs.analit.net".Format(user.AvaliableAddresses[0].Id)},
				new []{from}, 
				null);

			var handler = new DocSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);

			try {
				handler.CheckMime(message);
				Assert.Fail("Должно было возникнуть исключение MiniMailOnEmptyLetterException");
			}
			catch (MiniMailOnEmptyLetterException exception) {
				Assert.That(exception.Template, Is.EqualTo(ResponseTemplate.MiniMailOnEmptyLetter));
				SendErrorToProvider(handler, exception, message);
			}
		}

		[Test(Description = "проверям метод конвертации Mime.HtmlToText")]
		public void CheckHtmlToText()
		{
			var mime = Mime.Parse(@"..\..\Data\UnparseWithHtml.eml");
			Assert.IsNullOrEmpty(mime.BodyText);
			Assert.IsNotEmpty(mime.BodyHtml);

			var convertedText = mime.HtmlToText();
			Assert.IsNotEmpty(convertedText);

			var expectedText = @"
ДОБРЫЙ ДЕНЬ!
Наша фирма является поставщиком и официальным представителем органической косметики Натура Сиберика. Мы уже поставляем эту косметику в вашу аптеку по ул.Советская.
Наш склад находится в г.Белгород. Доставка в г Губкин и Ст.Оскол по средам.
На этой неделе машина будет в пятницу в связи с поступлением товара в четверг. Высылаю наши прайсы. и презентацию новинок.Наш прайс лист вы можете найти в ""аналитке""
С уважением,
ИП Деденко Виктория Владимировна
Белгород
8 960 628 51 32
";
			Assert.That(convertedText, Is.EqualTo(expectedText));
		}

		[Test(Description = "отправляем письмо, которого нет текстовой части, но есть html-body")]
		public void SendWithHtmlBody()
		{
			var client = TestClient.Create();
			var user = client.Users[0];
			
			SetUp(
				new List<TestUser> {user},
				null,
				"Это письмо пользователю",
				"Это текст письма пользователю",
				null);

			var mimeHtml = Mime.Parse(@"..\..\Data\UnparseWithHtml.eml");
			mimeHtml.MainEntity.To = _info.Mime.MainEntity.To;
			mimeHtml.MainEntity.From = _info.Mime.MainEntity.From;
			_info.Mime = mimeHtml;

			Process();
			
			using (new SessionScope()) {
				var mails = TestMailSendLog.Queryable.Where(l => l.User.Id == user.Id).ToList();
				Assert.That(mails.Count, Is.EqualTo(1));

				var mailLog = mails[0];
				Assert.That(mailLog.Mail.Supplier.Id, Is.EqualTo(_info.Supplier.Id));
				Assert.That(mailLog.Mail.Subject, Is.EqualTo("натура сиберика"));
				Assert.That(mailLog.Mail.Body, Is.StringStarting("\r\nДОБРЫЙ ДЕНЬ!\r\nНаша фирма является поставщиком и официальным представителем органической косметики Натура Сиберика. Мы уже поставляем эту косметику в вашу аптеку по ул.Советская."));
			}
		}

		[Test(Description = "отправляем письмо, которого нет текстовой части, но есть вложение")]
		public void SendWithEmptyBody()
		{
			var client = TestClient.Create();
			var user = client.Users[0];
			
			SetUp(
				new List<TestUser> {user},
				null,
				"Это письмо пользователю",
				"Это текст письма пользователю",
				null);

			var mimeEmptyBody = Mime.Parse(@"..\..\Data\UnparseWithEmptyBody.eml");
			Assert.IsNotNull(mimeEmptyBody.MainEntity.Subject);
			Assert.IsNotNull(mimeEmptyBody.BodyText);
			Assert.That(mimeEmptyBody.BodyText, Is.EqualTo(String.Empty));
			Assert.IsNull(mimeEmptyBody.BodyHtml);
			mimeEmptyBody.MainEntity.To = _info.Mime.MainEntity.To;
			mimeEmptyBody.MainEntity.From = _info.Mime.MainEntity.From;
			_info.Mime = mimeEmptyBody;

			Process();
			
			using (new SessionScope()) {
				var mails = TestMailSendLog.Queryable.Where(l => l.User.Id == user.Id).ToList();
				Assert.That(mails.Count, Is.EqualTo(1));

				var mailLog = mails[0];
				Assert.That(mailLog.Mail.Supplier.Id, Is.EqualTo(_info.Supplier.Id));
				Assert.That(mailLog.Mail.Subject, Is.EqualTo("Отказы по заявке № АХ1-1131222"));
				Assert.That(mailLog.Mail.Body, Is.Null);
				Assert.That(mailLog.Mail.Attachments.Count, Is.EqualTo(1));
				Assert.That(mailLog.Mail.Attachments[0].FileName, Is.EqualTo("K1795MZАХ1-1131222D120305.xls"));
			}
		}

	}
}