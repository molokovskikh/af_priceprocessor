using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Castle.ActiveRecord;
using Inforoom.Downloader;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Models;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using Test.Support.Logs;
using Test.Support.Suppliers;
using Common.Tools;

namespace PriceProcessor.Test.Waybills
{
	public class DocSourceHandlerTestInfo
	{
		public TestSupplier Supplier { get; set; }
		public TestRegion Region { get; set; }
		public IList<TestUser> Users { get; set; }
	}

	public class DocSourceHandlerForTesting : DocSourceHandler
	{
		public DocSourceHandlerForTesting(string mailbox, string password)
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
	public class DocSourceHandlerFixture
	{
		private DocSourceHandlerTestInfo _info;

		private bool IsEmlFile;

		[SetUp]
		public void DeleteDirectories()
		{
			_info = null;
			TestHelper.RecreateDirectories();
			ImapHelper.ClearImapFolder(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass, Settings.Default.IMAPSourceFolder);
		}

		private void PrepareSupplier(TestSupplier supplier, string from)
		{
			var group = supplier.ContactGroupOwner.AddContactGroup(ContactGroupType.MiniMails);
			group.AddContact(ContactType.Email, from);
			group.CreateAndFlush();
			supplier.Save();
		}

		private void SetUp(IList<TestUser> users, TestRegion region, string subject, string body, IList<string> fileNames)
		{
			var info = new DocSourceHandlerTestInfo();
			var supplier = TestSupplier.Create();

			var from = String.Format("{0}@supplier.test", supplier.Id);
			PrepareSupplier(supplier, from);
			info.Supplier = supplier;

			byte[] bytes;
			if (IsEmlFile)
				bytes = File.ReadAllBytes(fileNames[0]);
			else
			{
				var message = ImapHelper.BuildMessageWithAttachments(
					subject,
					body,
					users.Select(u => "{0}@docs.analit.net".Format(u.AvaliableAddresses[0].Id)).ToArray(),
					new []{from}, 
					fileNames != null ? fileNames.ToArray() : null);
				bytes = message.ToByteData();
			}

			ImapHelper.StoreMessage(
				Settings.Default.TestIMAPUser,
				Settings.Default.TestIMAPPass,
				Settings.Default.IMAPSourceFolder, 
				bytes);

			info.Region = region;
			info.Users = users;

			_info = info;
		}

		private void Process()
		{			
			Assert.That(_info, Is.Not.Null, "Перед обработкой должен быть вызван метод SetUp");
			var handler = new DocSourceHandlerForTesting(Settings.Default.TestIMAPUser, Settings.Default.TestIMAPPass);
			handler.Process();
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
			handler.Process();

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

	}
}