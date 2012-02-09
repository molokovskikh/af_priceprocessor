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
using Inforoom.PriceProcessor.Downloader;
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
			Recipients = new List<MailRecipient>();
			Users = new Dictionary<User, MailRecipient>();
		}

		public string SHA256MailHash { get; set; }
		public string SupplierEmails { get; set; }
		public string Subject { get; set; }
		public string Body { get; set; }
		public List<Supplier> Suppliers { get; set; }
		public List<MailRecipient> Recipients { get; set; }

		public Dictionary<User, MailRecipient> Users { get; set; }

		public void ParseRecipients(Mime mime)
		{
			ParseRecipientAddresses(mime.MainEntity.To);
			ParseRecipientAddresses(mime.MainEntity.Cc);
			ParseRecipientAddresses(mime.MainEntity.Bcc);

			if (Recipients.Count > 0) {
				foreach (var recipient in Recipients) {
					if (recipient.Status == RecipientStatus.Verified) {
						var users = recipient.GetUsers(Suppliers[0].RegionMask);
						if (users.Count > 0) {
							for (int i = users.Count-1; i > -1; i--) {
								var mails = MailSendLog.Queryable.Where(
									log => log.Mail.LogTime > DateTime.Now.AddDays(-1) && log.Mail.SHA256Hash == SHA256MailHash && log.User.Id == users[i].Id).ToList();
								if (mails.Count > 0)
									users.RemoveAt(i);
							}
							if (users.Count > 0)
								users.ForEach(u => AddUser(u, recipient));
							else 
								recipient.Status = RecipientStatus.Duplicate;
						}
						else 
							recipient.Status = RecipientStatus.NotAvailable;
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
			if (!Recipients.Exists(r => r.Equals(recipient)))
				Recipients.Add(recipient);
		}

		public void AddUser(User user, MailRecipient recipient)
		{
			if (!Users.Keys.Any(u => u.Id == user.Id))
				Users.Add(user, recipient);
		}

		public List<MailRecipient> VerifiedRecipients { get {return Recipients.Where(r => r.Status == RecipientStatus.Verified).ToList();} }

		public List<MailRecipient> DiscardedRecipients { get {return Recipients.Where(r => r.Status != RecipientStatus.Verified && r.Status != RecipientStatus.Duplicate).ToList();} }

		public string GetCauseList()
		{
			return DiscardedRecipients.Select(r => r.Email + " : " + r.Status.GetDescription()).Implode("\r\n");
		}

		public void ParseMime(Mime mime, AddressList fromSupplierList)
		{
			SHA256MailHash = mime.GetSHA256Hash();
			Subject = mime.MainEntity.Subject;
			SupplierEmails = fromSupplierList.Mailboxes.Select(mailbox => mailbox.EmailAddress).Implode();
			Suppliers = GetSuppliersFromList(fromSupplierList.Mailboxes);

			Body = mime.BodyText;
			if (String.IsNullOrWhiteSpace(Body))
				Body = mime.HtmlToText();
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

	public class DocSourceHandler : EMAILSourceHandler
	{

		// Email, из которого будут браться накладные и отказы
		private string _imapUser = Settings.Default.DocIMAPUser;

		// Пароль для указанного email-а
		private string _imapPassword = Settings.Default.DocIMAPPass;

		private MailContext _context;

		public uint VIPMailPayerId = 921;

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

			context.ParseMime(m, GetAddressList(m));

			Ping();

			if (String.IsNullOrEmpty(context.SHA256MailHash))
				throw new MiniMailOnEmptyLetterException("У письма не установлены тема и тело письма.");

			if (context.Suppliers.Count > 1)
				throw new EMailSourceHandlerException("Найдено несколько источников.");
			else 
				if (context.Suppliers.Count == 0)
					throw new MiniMailOnUnknownProviderException(
						"Для данного E-mail не найден контакт в группе 'Список E-mail, с которых разрешена отправка писем клиентам АналитФармация'",
						context.SupplierEmails);

			using (new SessionScope()) {
				context.ParseRecipients(m);
			}

			Ping();

			// Все хорошо, если кол-во вложений больше 0 и распознан только один адрес как корректный
			// Если не сопоставили с клиентом)
			if (context.Users.Count == 0)
			{
				if (context.Recipients.All(r => r.Status == RecipientStatus.Duplicate))
					throw new EMailSourceHandlerException("Письмо было отброшено как дубликат.");
				else 
					throw new MiniMailOnEmptyRecipientsException(
							"Не найден пользователь.", 
							context.GetCauseList());
			}
			else if (context.DiscardedRecipients.Count > 0) {
				SendErrorLetterToSupplier(
					new MiniMailOnEmptyRecipientsException(
						"Не найден пользователь.", 
						context.GetCauseList()),
					m);
			}

			if (m.MailSize() / 1024.0 / 1024.0 > Settings.Default.MaxMiniMailSize)
				throw new MiniMailOnMaxMailSizeException(
					"Размер письма больше максимально допустимого значения ({0} Мб).".Format(Settings.Default.MaxMiniMailSize));

			if (m.Attachments.Length > 0)
			{ 
				var nonAllowedExtension = false;
				var errorExtension = String.Empty;
				foreach (var attachment in m.GetValidAttachements()) {

					var fileName = attachment.GetFilename();
					if (!String.IsNullOrWhiteSpace(fileName) && !TemplateHolder.Values.ExtensionAllow(Path.GetExtension(fileName)))
					{
						nonAllowedExtension = true;
						errorExtension = Path.GetExtension(fileName);
						break;
					}

				}
				if (nonAllowedExtension) {
					throw new MiniMailOnAllowedExtensionsException(
						"Письмо содержит вложение недопустимого типа.",
						errorExtension,
						TemplateHolder.Values.AllowedMiniMailExtensions);
				}
			}

			_context = context;
		}
		
		public void SendErrorLetterToSupplier(MiniMailException e, Mime sourceLetter)
		{
			try
			{
				e.MailTemplate = TemplateHolder.GetTemplate(e.Template);

				if (e.MailTemplate.IsValid()) {
					var FromList = GetAddressList(sourceLetter);

					var _from = new AddressList();
					_from.Parse("farm@analit.net");

					var responseMime = new Mime();
					responseMime.MainEntity.From = _from;
	#if DEBUG
					var toList = new AddressList { new MailboxAddress(Settings.Default.SMTPUserFail) };				
					responseMime.MainEntity.To = toList;
	#else
					responseMime.MainEntity.To = FromList;
	#endif
					responseMime.MainEntity.Subject = e.MailTemplate.Subject;
					responseMime.MainEntity.ContentType = MediaType_enum.Multipart_mixed;

					var testEntity  = responseMime.MainEntity.ChildEntities.Add();
					testEntity.ContentType = MediaType_enum.Text_plain;
					testEntity.ContentTransferEncoding = ContentTransferEncoding_enum.QuotedPrintable;
					testEntity.DataText = e.GetBody(sourceLetter);

					var attachEntity  = responseMime.MainEntity.ChildEntities.Add();
					attachEntity.ContentType = MediaType_enum.Application_octet_stream;
					attachEntity.ContentTransferEncoding = ContentTransferEncoding_enum.Base64;
					attachEntity.ContentDisposition = ContentDisposition_enum.Attachment;
					attachEntity.ContentDisposition_FileName = (!String.IsNullOrEmpty(sourceLetter.MainEntity.Subject)) ? sourceLetter.MainEntity.Subject + ".eml" : "Unrec.eml";
					attachEntity.Data = sourceLetter.ToByteData();

					LumiSoft.Net.SMTP.Client.SmtpClientEx.QuickSendSmartHost(Settings.Default.SMTPHost, 25, String.Empty, responseMime);
				}
				else 
					_logger.ErrorFormat("Для шаблона '{0}' не установлено значение", e.Template.GetDescription());

			}
			catch (Exception exception) {
				_logger.WarnFormat("Ошибка при отправке письма поставщику: {0}", exception);
			}
		}

		protected override void ErrorOnMessageProcessing(Mime m, AddressList from, EMailSourceHandlerException e)
		{
			try
			{
				if (e is MiniMailException) {
					//отправляем письмо поставщику
					SendErrorLetterToSupplier((MiniMailException)e, m);
				}
				else 
					//отправляем письмо в tech для разбора
					SendUnrecLetter(m, from, e);
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
				Subject = _context.Subject,
				Body = _context.Body,
				LogTime = DateTime.Now,
				SHA256Hash = _context.SHA256MailHash,
				IsVIPMail = VIPMailPayerId == _context.Suppliers[0].Payer
			};

			var attachments = m.GetValidAttachements();
			foreach (var entity in attachments) {
				SaveAttachement(entity);
				mail.Attachments.Add(new Attachment(mail, CurrFileName));
			}
			mail.Size = (uint)(_context.Body.Length + mail.Attachments.Sum(a => a.Size));

			foreach (var verifyRecipient in _context.VerifiedRecipients) {
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

	}
}