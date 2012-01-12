using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Inforoom.Downloader;
using Inforoom.PriceProcessor;
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
				new List<string> {@"..\..\Data\Waybills\0000470553.dbf"});

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
				Assert.That(mail.Size, Is.EqualTo(53928));

				var attachment = mail.Attachments[0];
				Assert.That(attachment.FileName, Is.EqualTo("0000470553.dbf"));
				Assert.That(attachment.Extension, Is.EqualTo(".dbf"));
				Assert.That(attachment.Size, Is.EqualTo(new FileInfo(@"..\..\Data\Waybills\0000470553.dbf").Length));

				var attachLogs = TestAttachmentSendLog.Queryable.Where(l => l.User.Id == user.Id).ToList();
				Assert.That(attachLogs.Count, Is.EqualTo(1));

				var attachLog = attachLogs[0];
				Assert.That(attachLog.UpdateLogEntry, Is.Null);
				Assert.That(attachLog.Committed, Is.False);
				Assert.That(attachLog.Attachment.Id, Is.EqualTo(attachment.Id));

				Assert.That(File.Exists(Path.Combine(Settings.Default.AttachmentPath, attachment.GetSaveFileName())), Is.True);
			}
		}

	}
}