using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Castle.Core;
using Common.MySql;
using Common.Tools;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.Mime;
using MySql.Data.MySqlClient;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace Inforoom.Downloader
{

	public class MailContext
	{
		public MailContext()
		{
			FullRecipients = new List<MailRecipient>();
			Users = new Dictionary<User, MailRecipient>();
		}

		public string SHA256MailHash { get; set; }
		public string SupplierEmails { get; set; }
		public List<Supplier> Suppliers { get; set; }
		public List<MailRecipient> FullRecipients { get; set; }
		public List<MailRecipient> VerifyRecipients { get; set; }
		public Dictionary<User, MailRecipient> Users { get; set; }

		public void ParseRecipients(Mime mime)
		{
			ParseRecipientAddresses(mime.MainEntity.To);
			ParseRecipientAddresses(mime.MainEntity.Cc);
			ParseRecipientAddresses(mime.MainEntity.Bcc);

			if (FullRecipients.Count > 0) {
				VerifyRecipients = new List<MailRecipient>();
				foreach (var fullRecipient in FullRecipients) {
					var users = fullRecipient.GetUsers(Suppliers[0].RegionMask);
					if (users.Count > 0) {
						VerifyRecipients.Add(fullRecipient);
						users.ForEach(u => AddUser(u, fullRecipient));
					}
				}
			}
		}

		private void ParseRecipientAddresses(AddressList addressList)
		{
			if (addressList == null)
				return;
			// Пробегаемся по всем адресам TO и ищем адрес вида 
			// <\d+@docs.analit.net> или <\d+@docs.analit.net>
			foreach(var mailbox in  addressList.Mailboxes) {
				var mail = EMAILSourceHandler.GetCorrectEmailAddress(mailbox.EmailAddress);
				var recipient = MailRecipient.Parse(mail);
				if (recipient != null)
					AddRecipient(recipient);
			}
		}

		public void AddRecipient(MailRecipient recipient)
		{
			if (!FullRecipients.Exists(r => r.Equals(recipient)))
				FullRecipients.Add(recipient);
		}

		public void AddUser(User user, MailRecipient recipient)
		{
			if (!Users.Keys.Any(u => u.Id == user.Id))
				Users.Add(user, recipient);
		}

	}

	public class DocSourceHandler : EMAILSourceHandler
	{

		// Email, из которого будут браться накладные и отказы
		private string _imapUser = Settings.Default.DocIMAPUser;

		// Пароль для указанного email-а
		private string _imapPassword = Settings.Default.DocIMAPPass;

		private MailContext _context;

		public DocSourceHandler()
		{
			SourceType = "Doc";
		}

		public DocSourceHandler(string imapUser, string imapPassword)
			: this()
		{
			if (!String.IsNullOrEmpty(imapUser) && !String.IsNullOrEmpty(imapPassword))
			{
				_imapUser = imapUser;
				_imapPassword = imapPassword;
			}
		}

		protected override void IMAPAuth(IMAP_Client client)
		{
			client.Authenticate(_imapUser, _imapPassword);
		}

		public override void CheckMime(Mime m)
		{
			_context = null;

			var context = new MailContext();

			Ping();

			context.SHA256MailHash = m.GetSHA256Hash();
			var fromSupplierList = GetAddressList(m);
			context.SupplierEmails = fromSupplierList.Mailboxes.Select(mailbox => mailbox.EmailAddress).Implode();
			context.Suppliers = GetSuppliersFromList(fromSupplierList.Mailboxes);

			Ping();

			if (context.Suppliers.Count > 1)
				throw new EMailSourceHandlerException("Найдено несколько источников.");
			else 
				if (context.Suppliers.Count == 0)
					throw new EmailByMiniMails(
						"Для данного E-mail не найден контакт в группе 'Список E-mail, с которых разрешена отправка писем клиентам АналитФармация'", 
						ResponseTemplate.MiniMailOnUnknownProvider);

			using (new SessionScope()) {
				context.ParseRecipients(m);
			}

			Ping();

			// Все хорошо, если кол-во вложений больше 0 и распознан только один адрес как корректный
			// Если не сопоставили с клиентом)
			if (context.Users.Count == 0)
			{
				throw new EmailByMiniMails(
						"Не найден пользователь.", 
						ResponseTemplate.MiniMailOnEmptyRecipients);
			}
			if (m.Attachments.Length > 0)
			{ 
				var attachmentsIsBigger = false;
				var nonAllowedExtension = false;
				foreach (var attachment in m.Attachments) {

					var fileName = attachment.GetFilename();
					if (!String.IsNullOrWhiteSpace(fileName) && !TemplateHolder.Values.ExtensionAllow(Path.GetExtension(fileName)))
					{
						nonAllowedExtension = true;
						break;
					}

					if ((attachment.Data.Length / 1024.0) > Settings.Default.MaxWaybillAttachmentSize)
					{
						attachmentsIsBigger = true;
						break;
					}
				}
				if (nonAllowedExtension) {
					throw new EmailByMiniMails(
						"Письмо содержит вложение недопустимого типа.",
						ResponseTemplate.MiniMailOnAllowedExtensions);
				}
				if (attachmentsIsBigger) {
					throw new EmailByMiniMails(
						String.Format("Письмо содержит вложение размером больше максимально допустимого значения ({0} Кб).",
						              Settings.Default.MaxWaybillAttachmentSize),
						ResponseTemplate.MiniMailOnMaxAttachment);
				}
			}

			_context = context;
		}
		
		protected override void ErrorOnMessageProcessing(Mime m, AddressList from, EMailSourceHandlerException e)
		{
			try
			{
				if (String.IsNullOrEmpty(e.Body))
				{
					SendUnrecLetter(m, from, e);
					return;
				}

				var subject = e.Subject;
				var body = e.Body;
				var message = e.Message;
				if (e is EmailFromUnregistredMail)
				{
					subject = Settings.Default.ResponseDocSubjectTemplateOnUnknownProvider;
					body = Settings.Default.ResponseDocBodyTemplateOnUnknownProvider;
					message = "Для данного E-mail не найден контакт в ";
				}
				var attachments = m.Attachments.Where(a => !String.IsNullOrEmpty(a.GetFilename())).Aggregate("", (s, a) => s + String.Format("\"{0}\"\r\n", a.GetFilename()));

				WaybillSourceHandler.SendErrorLetterToProvider(
				    from, 
				    subject, 
				    body, m);

				if (e is EmailFromUnregistredMail)
				{
					var ms = new MemoryStream(m.ToByteData());
					FailMailSend(m.MainEntity.Subject, from.ToAddressListString(),
						m.MainEntity.To.ToAddressListString(), m.MainEntity.Date, ms, attachments, e.Body);
				}

				var comment = String.Format(@"{0} 
Отправители     : {1}
Получатели      : {2}
Список вложений : 
{3}
Тема письма поставщику : {4}
Тело письма поставщику : 
{5}",
					message,
					from.ToAddressListString(),
					m.MainEntity.To.ToAddressListString(),
					attachments,
					subject, 
					body);

				_logger.WarnFormat("Нераспознанное письмо: {0}", comment);
			}
			catch (Exception exMatch)
			{
				_logger.Error("Не удалось отправить нераспознанное письмо", exMatch);
			}
		}

		protected override string GetFailMail()
		{
			return Settings.Default.DocumentFailMail;
		}

		protected override void SendUnrecLetter(Mime m, AddressList fromList, EMailSourceHandlerException e)
		{
			try
			{
				var attachments = m.Attachments.Where(a => !String.IsNullOrEmpty(a.GetFilename())).Aggregate("", (s, a) => s + String.Format("\"{0}\"\r\n", a.GetFilename()));
				var ms = new MemoryStream(m.ToByteData());
				FailMailSend(m.MainEntity.Subject, fromList.ToAddressListString(), 
					m.MainEntity.To.ToAddressListString(), m.MainEntity.Date, ms, attachments, e.Message);
			}
			catch (Exception exMatch)
			{
				_logger.Error("Не удалось отправить нераспознанное письмо", exMatch);
			}
		}

		protected override void ProcessAttachs(Mime m, AddressList FromList)
		{
			if (_context == null)
				throw new EMailSourceHandlerException("Не установлен контекст для обработки письма");

			Cleanup();

			var mail = new Mail {
				Supplier = _context.Suppliers[0],
				SupplierEmail = _context.SupplierEmails,
				Subject = m.MainEntity.Subject,
				Body = m.BodyText,
				LogTime = DateTime.Now,
				SHA256Hash = _context.SHA256MailHash
			};

			var attachments = m.GetValidAttachements();
			foreach (var entity in attachments) {
				SaveAttachement(entity);
				mail.Attachments.Add(new Attachment(mail, CurrFileName));
			}
			mail.Size = (uint)(mail.Body.Length + mail.Attachments.Sum(a => a.Size));

			foreach (var verifyRecipient in _context.VerifyRecipients) {
				verifyRecipient.Mail = mail;
				mail.MailRecipients.Add(verifyRecipient);
			}

			var mailLogs = _context.Users.Select(i => new MailSendLog(i.Key, i.Value)).ToList();

			var attachmentLogs = new List<AttachmentSendLog>();
			foreach (var attachement in mail.Attachments) {
				attachmentLogs.AddRange(_context.Users.Select(i => new AttachmentSendLog{Attachment = attachement, User = i.Key}));
			}

			Ping();

			using (var transaction = new TransactionScope(OnDispose.Rollback)) {
				mail.Create();
				mail.Attachments.ForEach(a =>
					File.Copy(
						a.LocalFileName,
						Path.Combine(Settings.Default.AttachmentPath, a.GetSaveFileName())));

				mailLogs.ForEach(l => l.Create());
				attachmentLogs.ForEach(l => l.Create());

				transaction.VoteCommit();
			}

		}

		private List<Supplier> GetSuppliersFromList(MailboxAddress[] mailboxes)
		{
			var dtSuppliers = new DataTable();

			using(var connection = new MySqlConnection(Literals.ConnectionString()))
			{
				connection.Open();
				var adapter = new MySqlDataAdapter(@"
select
  s.Id
from
  future.Suppliers s
  inner join contacts.contact_groups cg on cg.ContactGroupOwnerId = s.ContactGroupOwnerId and cg.Type = 10
  inner join contacts.contacts c on c.ContactOwnerId = cg.Id and c.Type = 0
where
  c.ContactText in (" + mailboxes.Select(m => "'" + m.EmailAddress +"'").Implode() + ") group by s.Id", 
					  connection);
				adapter.Fill(dtSuppliers);
			}

			var result = new List<Supplier>();
			using (new SessionScope()) {
				foreach (DataRow dataRow in dtSuppliers.Rows) {
					result.Add(Supplier.Find(Convert.ToUInt32(dataRow["Id"])));
				}
			}

			return result;
		}

	}
}