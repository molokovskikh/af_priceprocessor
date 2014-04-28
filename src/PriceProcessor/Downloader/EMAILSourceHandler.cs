using System;
using System.IO;
using System.Data;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Helpers;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.Mime;
using LumiSoft.Net.IMAP;
using Inforoom.Common;
using LumiSoft.Net.SMTP.Client;
using FileHelper = Inforoom.PriceProcessor.FileHelper;

namespace Inforoom.Downloader
{
	public class EMAILSourceHandler : BasePriceSourceHandler, IIMAPReader
	{
		protected IMAPHandler IMAPHandler;

		public EMAILSourceHandler()
		{
			IMAPHandler = new IMAPHandler(this);
			SourceType = "EMAIL";
		}

		public override void ProcessData()
		{
			IMAPHandler.ProcessIMAPFolder();
		}

		public void ProcessBrokenMessage(IMAP_FetchItem item, IMAP_FetchItem[] OneItem, Exception ex)
		{
			_logger.Error("Ошибка при обработке письма", ex);
			MemoryStream ms = null;
			if (OneItem != null && OneItem.Length > 0 && OneItem[0].MessageData != null)
				ms = new MemoryStream(OneItem[0].MessageData);
			ErrorMailSend(item.UID, ex.ToString(), ms);
		}

		public void PingReader()
		{
			Ping();
		}

		public virtual void IMAPAuth(IMAP_Client c)
		{
			c.Authenticate(Settings.Default.IMAPUser, Settings.Default.IMAPPass);
		}

		public void ProcessMime(Mime m)
		{
			var from = MimeEntityExtentions.GetAddressList(m);
			m = UueHelper.ExtractFromUue(m, DownHandlerPath);
			FillSourcesTable();
			try {
				CheckMime(m);
				ProcessAttachs(m, from);
			}
			catch (EMailSourceHandlerException e) {
				// Формируем список приложений, чтобы использовать
				// его при отчете о нераспознанном письме
				ErrorOnMessageProcessing(m, from, e);
			}
		}

		protected virtual void ErrorOnMessageProcessing(Mime m, AddressList from, EMailSourceHandlerException exception)
		{
			SendUnrecLetter(m, from, exception);
		}

		protected virtual void SendUnrecLetter(Mime m, AddressList from, EMailSourceHandlerException exception)
		{
			try {
				var attachments = m.Attachments.Where(a => !String.IsNullOrEmpty(a.GetFilename())).Aggregate("", (s, a) => s + String.Format("\"{0}\"\r\n", a.GetFilename()));
				var ms = new MemoryStream(m.ToByteData());
#if !DEBUG
				SmtpClientEx.QuickSendSmartHost(
					Settings.Default.SMTPHost,
					25,
					Environment.MachineName,
					Settings.Default.ServiceMail,
					new[] { Settings.Default.UnrecLetterMail },
					ms);
#endif
				FailMailSend(m.MainEntity.Subject, from.ToAddressListString(),
					m.MainEntity.To.ToAddressListString(), m.MainEntity.Date, ms, attachments, exception.Message);
				DownloadLogEntity.Log((ulong)PriceSourceType.EMail, String.Format("Письмо не распознано.Причина : {0}; Тема :{1}; От : {2}",
					exception.Message, m.MainEntity.Subject, from.ToAddressListString()));
			}
			catch (Exception exMatch) {
				_logger.Error("Не удалось отправить нераспознанное письмо", exMatch);
			}
		}

		protected virtual void ProcessAttachs(Mime m, AddressList fromList)
		{
			//Один из аттачментов письма совпал с источником, иначе - письмо не распознано
			var matched = false;

			var attachments = m.GetValidAttachements();
			foreach (var entity in attachments) {
				var attachmentFileName = SaveAttachement(entity);
				UnPack(m, ref matched, fromList, attachmentFileName);
				Cleanup();
			}

			if (!matched)
				throw new EMailSourceHandlerException("Не найден источник.");
		}

		/// <summary>
		/// Проверяет, что письмо содержит вложения
		/// </summary>
		public virtual void CheckMime(Mime m)
		{
			if (m.Attachments.Length == 0)
				throw new EMailSourceHandlerException("Письмо не содержит вложений.");
		}

		/// <summary>
		/// Происходит разбор собственно вложения и сверка его с источниками
		/// </summary>
		private void UnPack(Mime m, ref bool Matched, AddressList FromList, string ShortFileName)
		{
			//Раньше не проверялся весь список From, теперь это делается. Туда же добавляется и Sender
			foreach (var mbFrom in FromList.Mailboxes) {
				//Раньше не проверялся весь список TO, теперь это делается
				foreach (var mba in m.MainEntity.To.Mailboxes) {
					var drLS = dtSources.Select(String.Format("({0} = '{1}') and ({2} like '*{3}*')",
						SourcesTableColumns.colEMailTo, MimeEntityExtentions.GetCorrectEmailAddress(mba.EmailAddress),
						SourcesTableColumns.colEMailFrom, mbFrom.EmailAddress));
					foreach (DataRow drS in drLS) {
						if ((drS[SourcesTableColumns.colPriceMask] is String) &&
							!String.IsNullOrEmpty(drS[SourcesTableColumns.colPriceMask].ToString())) {
							var priceMask = (string)drS[SourcesTableColumns.colPriceMask];
							var priceMaskIsMatched = FileHelper.CheckMask(ShortFileName, priceMask);
							if (priceMaskIsMatched) {
								SetCurrentPriceCode(drS);

								// Пробуем разархивировать
								var correctArchive = CheckFile(drS["ArchivePassword"].ToString());

								if (correctArchive) {
									string extrFile;
									if (ProcessPriceFile(CurrFileName, out extrFile, (ulong)PriceSourceType.EMail)) {
										Matched = true;
										LogDownloadedPrice((ulong)PriceSourceType.EMail, Path.GetFileName(CurrFileName), extrFile);
									}
									else {
										LogDownloaderFail((ulong)PriceSourceType.EMail, "Не удалось обработать файл '" +
											Path.GetFileName(CurrFileName) + "'",
											Path.GetFileName(CurrFileName));
									}
								}
								else {
									LogDownloaderFail((ulong)PriceSourceType.EMail, "Не удалось распаковать файл '" +
										Path.GetFileName(CurrFileName) + "'. Файл поврежден",
										Path.GetFileName(CurrFileName));
								}
								drS.Delete();
							}
						}
					}
					dtSources.AcceptChanges();
				} //foreach (MailboxAddress mba in m.MainEntity.To.Mailboxes)
			} //foreach (MailboxAddress mbFrom in FromList.Mailboxes)
		}

		protected override void CopyToHistory(UInt64 PriceID)
		{
			var historyFileName = Path.Combine(DownHistoryPath, PriceID + ".eml");
			var savedFile = Path.Combine(DownHandlerPath, PriceID + ".eml");
			try {
				IMAPHandler.Message.ToFile(savedFile);
				File.Copy(savedFile, historyFileName);
				File.Delete(savedFile);
			}
			catch {
			}
		}

		protected bool CheckFile()
		{
			return CheckFile(null);
		}

		private bool CheckFile(string archivePassword)
		{
			var fileName = CurrFileName;
			var tempExtractDir = CurrFileName + ExtrDirSuffix;

			//Является ли скачанный файл корректным, если нет, то обрабатывать не будем
			if (ArchiveHelper.IsArchive(fileName)) {
				if (ArchiveHelper.TestArchive(fileName, archivePassword)) {
					try {
						FileHelper.ExtractFromArhive(fileName, tempExtractDir, archivePassword);
						return true;
					}
					catch (ArchiveHelper.ArchiveException) {
						return false;
					}
				}
				return false;
			}
			return true;
		}

		protected string SaveAttachement(MimeEntity ent)
		{
			var attachmentFileName = GetShortFileNameFromAttachement(ent);
			CurrFileName = DownHandlerPath + attachmentFileName;
			using (var fs = new FileStream(CurrFileName, FileMode.Create))
				ent.DataToStream(fs);
			return attachmentFileName;
		}

		protected static string GetShortFileNameFromAttachement(MimeEntity ent)
		{
			var filename = ent.GetFilename();
			if (filename == null)
				return String.Empty;
			return filename;
		}

		public void SendErrorLetterToSupplier(EMailSourceHandlerException e, Mime sourceLetter)
		{
			try {
				var miniMailException = e as MiniMailException;
				if (miniMailException != null)
					e.MailTemplate = TemplateHolder.GetTemplate(miniMailException.Template);

				if (e.MailTemplate.IsValid()) {
					var FromList = MimeEntityExtentions.GetAddressList(sourceLetter);

					var from = new AddressList();

					@from.Parse("tech@analit.net");

					var responseMime = new Mime();
					responseMime.MainEntity.From = @from;
#if DEBUG
					var toList = new AddressList { new MailboxAddress(Settings.Default.SMTPUserFail) };
					responseMime.MainEntity.To = toList;
#else
					responseMime.MainEntity.To = FromList;
#endif
					responseMime.MainEntity.Subject = e.MailTemplate.Subject;
					responseMime.MainEntity.ContentType = MediaType_enum.Multipart_mixed;

					var testEntity = responseMime.MainEntity.ChildEntities.Add();
					testEntity.ContentType = MediaType_enum.Text_plain;
					testEntity.ContentTransferEncoding = ContentTransferEncoding_enum.QuotedPrintable;
					testEntity.DataText = e.GetBody(sourceLetter);

					var attachEntity = responseMime.MainEntity.ChildEntities.Add();
					attachEntity.ContentType = MediaType_enum.Application_octet_stream;
					attachEntity.ContentTransferEncoding = ContentTransferEncoding_enum.Base64;
					attachEntity.ContentDisposition = ContentDisposition_enum.Attachment;
					attachEntity.ContentDisposition_FileName = (!String.IsNullOrEmpty(sourceLetter.MainEntity.Subject)) ? sourceLetter.MainEntity.Subject + ".eml" : "Unrec.eml";
					attachEntity.Data = sourceLetter.ToByteData();

					Send(responseMime);
				}
				else
					_logger.ErrorFormat("Для исключения '{0}' не установлен шаблон", e.GetType());
			}
			catch (Exception exception) {
				_logger.WarnFormat("Ошибка при отправке письма поставщику: {0}", exception);
			}
		}

		//для того что бы перекрыть в тестах
		protected virtual void Send(Mime mime)
		{
			SmtpClientEx.QuickSendSmartHost(Settings.Default.SMTPHost, 25, String.Empty, mime);
		}
	}

	public class EmailFromUnregistredMail : EMailSourceHandlerException
	{
		public EmailFromUnregistredMail(string message) : base(message)
		{
		}

		public EmailFromUnregistredMail(string message, string subject, string body) : base(message, subject, body)
		{
		}
	}

	public class EMailSourceHandlerException : Exception
	{
		public EMailSourceHandlerException(string message) : base(message)
		{
		}

		public EMailSourceHandlerException(string message, string subject, string body) : base(message)
		{
			MailTemplate = new MailTemplate(ResponseTemplate.MiniMailOnEmptyRecipients, subject, body);
		}

		public MailTemplate MailTemplate { get; set; }

		public virtual string GetBody(Mime mime)
		{
			return String.Format(MailTemplate.Body, mime.MainEntity.Subject);
		}
	}

	//Класс исключений для ситуации возникновения "Письмо было отброшено как дубликат."
	public class EmailDoubleMessageException : EMailSourceHandlerException
	{
		public EmailDoubleMessageException(string message) : base(message)
		{
		}
	}

	/// <summary>
	/// Возникает при обработке мини-почты, когда LumiSoft не смог распарсить список отправителей
	/// </summary>
	public class FromParseException : EMailSourceHandlerException
	{
		public FromParseException(string message) : base(message)
		{
		}
	}
}