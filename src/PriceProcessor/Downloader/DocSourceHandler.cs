using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Castle.Core;
using Common.Tools;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.Mime;

namespace Inforoom.Downloader
{
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
			if (!String.IsNullOrEmpty(imapUser) && !String.IsNullOrEmpty(imapPassword)) {
				_imapUser = imapUser;
				_imapPassword = imapPassword;
			}
		}

		public override void IMAPAuth(IMAP_Client client)
		{
			client.Authenticate(_imapUser, _imapPassword);
		}

		public override void CheckMime(Mime m)
		{
			_context = null;

			var context = new MailContext();

			Ping();

			context.ParseMime(m, MimeEntityExtentions.GetAddressList(m));

			Ping();

			if (String.IsNullOrEmpty(context.SHA256MailHash))
				throw new MiniMailOnEmptyLetterException("У письма не установлены тема и тело письма.");

			if (context.Suppliers.Count > 1)
				throw new EMailSourceHandlerException("Найдено несколько источников.");

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
			if (context.Users.Count == 0) {
				if (context.Recipients.All(r => r.Status == RecipientStatus.Duplicate))
					throw new EmailDoubleMessageException("Письмо было отброшено как дубликат. " + context.SupplierEmails + " " + context.Subject);
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

			if (m.Attachments.Length > 0) {
				var nonAllowedExtension = false;
				var errorExtension = String.Empty;
				foreach (var attachment in m.GetValidAttachements()) {
					var fileName = attachment.GetFilename();
					var extension = Path.GetExtension(fileName);
					if (!context.IsValidExtension(extension)) {
						nonAllowedExtension = true;
						errorExtension = extension;
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

		protected override void ErrorOnMessageProcessing(Mime m, AddressList from, EMailSourceHandlerException e)
		{
			try {
				//отправляем письмо поставщику
				if (e is MiniMailException) {
					SendErrorLetterToSupplier(e, m);
				}
				else if (e is EmailDoubleMessageException)
					//обрабатываем случай сообщений-дубликатов - логирование как Warning
					_logger.WarnFormat("Произошла отправка дубликата письма: {0}", e);
				else if (e is FromParseException)
					//обрабатываем случай с проблемой разбора списка отправителя - логирование как Warning
					_logger.Warn("Не разобран список отправителей письма", e);
				else
					//отправляем письмо в tech для разбора
					SendUnrecLetter(m, from, e);
			}
			catch (Exception exMatch) {
				_logger.Error("Не удалось отправить нераспознанное письмо", exMatch);
			}
		}

		protected override string GetFailMail()
		{
			return Settings.Default.DocumentFailMail;
		}

		protected override void SendUnrecLetter(Mime m, AddressList fromList, EMailSourceHandlerException e)
		{
			try {
				var attachments = m.Attachments.Where(a => !String.IsNullOrEmpty(a.GetFilename())).Aggregate("", (s, a) => s + String.Format("\"{0}\"\r\n", a.GetFilename()));
				var ms = new MemoryStream(m.ToByteData());
				FailMailSend(m.MainEntity.Subject, fromList.ToAddressListString(),
					m.MainEntity.To.ToAddressListString(), m.MainEntity.Date, ms, attachments, e.Message);
			}
			catch (Exception exMatch) {
				_logger.Error("Не удалось отправить нераспознанное письмо", exMatch);
			}
		}

		protected override void ProcessAttachs(Mime m, AddressList fromList)
		{
			if (_context == null)
				throw new EMailSourceHandlerException("Не установлен контекст для обработки письма");

			Cleanup();

			var mail = new Mail(_context);

			var attachments = m.GetValidAttachements();
			foreach (var entity in attachments) {
				SaveAttachement(entity);
				mail.Attachments.Add(new Attachment(mail, CurrFileName));
			}
			mail.Size = (uint)(_context.BodyLength + mail.Attachments.Sum(a => a.Size));

			foreach (var verifyRecipient in _context.VerifiedRecipients) {
				verifyRecipient.Mail = mail;
				mail.MailRecipients.Add(verifyRecipient);
			}

			var mailLogs = _context.Users.Select(i => new MailSendLog(i.Key, i.Value)).ToList();

			var attachmentLogs = new List<AttachmentSendLog>();
			foreach (var attachement in mail.Attachments) {
				attachmentLogs.AddRange(_context.Users.Select(i => new AttachmentSendLog { Attachment = attachement, User = i.Key }));
			}

			Ping();

			using (var transaction = new TransactionScope(OnDispose.Rollback)) {
				mail.Save();
				mail.Attachments.Each(a =>
					File.Copy(
						a.LocalFileName,
						Path.Combine(Settings.Default.AttachmentPath, a.GetSaveFileName())));

				mailLogs.ForEach(l => l.Save());
				attachmentLogs.ForEach(l => l.Save());

				transaction.VoteCommit();
			}
		}
	}
}