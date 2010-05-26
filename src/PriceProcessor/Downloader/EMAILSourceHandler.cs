using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor.Downloader;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.Mime;
using Inforoom.PriceProcessor.Properties;
using LumiSoft.Net.IMAP;
using Inforoom.Common;
using FileHelper = Inforoom.PriceProcessor.FileHelper;
using Inforoom.PriceProcessor;
using System.Net.Mail;


namespace Inforoom.Downloader
{
	public class EMAILSourceHandler : BasePriceSourceHandler
    {
		//UID текущего обрабатываемого письма
		protected int currentUID;

    	private Mime _message;

		public EMAILSourceHandler()
		{
			sourceType = "EMAIL";
		}

		protected string GetCorrectEmailAddress(string Source)
		{
			return Source.Replace("'", String.Empty).Trim();
		}

		protected bool IsMailAddress(string address)
		{
			try
			{
#pragma warning disable 168
				var mailAddress = new MailAddress(address);
#pragma warning restore 168
				return true;
			}
			catch (FormatException)
			{
				return false;
			}
		}

		protected override void ProcessData()
		{
			var imapSourceFolder = Settings.Default.IMAPSourceFolder;
			using (var imapClient = new IMAP_Client())
			{
				imapClient.Connect(Settings.Default.IMAPHost, 143);
				IMAPAuth(imapClient);
				imapClient.SelectFolder(imapSourceFolder);

				try
				{
					IMAP_FetchItem[] items;
					do
					{
						Ping();
						var sequence_set = new IMAP_SequenceSet();
						sequence_set.Parse("1:*", long.MaxValue);
						items = imapClient.FetchMessages(sequence_set, IMAP_FetchItem_Flags.UID, false, false);
						Ping();

						if (items == null || items.Length == 0)
							continue;

						foreach (var item in items)
						{
							IMAP_FetchItem[] OneItem = null;
							try
							{
								var sequence_Mess = new IMAP_SequenceSet();
								sequence_Mess.Parse(item.UID.ToString(), long.MaxValue);
								OneItem = imapClient.FetchMessages(sequence_Mess, IMAP_FetchItem_Flags.Message, false, true);
								_message = Mime.Parse(OneItem[0].MessageData);
								currentUID = item.UID;
								Ping();
								ProcessMime(_message);
							}
							catch (Exception ex)
							{
								_logger.Error("Ошибка при обработке письма", ex);
								_message = null;
								SendBrokenMessage(item, OneItem, ex);
							}
						}

						//Производим удаление писем
						var sequence = new IMAP_SequenceSet();

						sequence.Parse(String.Join(",", items.Select(i => i.UID.ToString()).ToArray()), long.MaxValue);
						imapClient.DeleteMessages(sequence, true);
					}
					while (items != null && items.Length > 0);
				}
				finally
				{
					_message = null;
				}
			}
		}

		private void SendBrokenMessage(IMAP_FetchItem item, IMAP_FetchItem[] OneItem, Exception ex)
		{
			MemoryStream ms = null;
			if (OneItem != null && OneItem.Length > 0 && OneItem[0].MessageData != null)
				ms = new MemoryStream(OneItem[0].MessageData);
			ErrorMailSend(item.UID, ex.ToString(), ms);
		}

		protected virtual void IMAPAuth(IMAP_Client c)
		{
			c.Authenticate(Settings.Default.IMAPUser, Settings.Default.IMAPPass);
		}

		private void ProcessMime(Mime m)
		{
			var from = GetAddressList(m);

			m = UueHelper.ExtractFromUue(m, DownHandlerPath);
			try
			{
				CheckMime(m);
			}
			catch(EMailSourceHandlerException e)
			{
				ErrorOnCheckMime(m, from, e);
				return;

			}
			FillSourcesTable();
			try
			{
				ProcessAttachs(m, from);
			}
			catch (EMailSourceHandlerException e)
			{
				// Формируем список приложений, чтобы использовать 
				// его при отчете о нераспознанном письме
				ErrorOnProcessAttachs(m, from, e);
				return;
			}
		}

		protected virtual void ErrorOnProcessAttachs(Mime m, AddressList from, EMailSourceHandlerException exception)
		{
			SendUnrecLetter(m, from, exception);
		}

		
		protected virtual void ErrorOnCheckMime(Mime m, AddressList from, EMailSourceHandlerException exception)
		{
			SendUnrecLetter(m, from, exception);
		}

		protected virtual void SendUnrecLetter(Mime m, AddressList from, EMailSourceHandlerException exception)
		{
			try
			{
				var attachments = m.Attachments.Where(a => !String.IsNullOrEmpty(a.GetFilename())).Aggregate("", (s, a) => s + String.Format("\"{0}\"\r\n", a.GetFilename()));
				var ms = new MemoryStream(m.ToByteData());
				try
				{
					LumiSoft.Net.SMTP.Client.SmtpClientEx.QuickSendSmartHost(
						Settings.Default.SMTPHost,
						25,
						Environment.MachineName,
						Settings.Default.ServiceMail,
						new[] { Settings.Default.UnrecLetterMail },
						ms);
				}
				catch { }
				FailMailSend(m.MainEntity.Subject, from.ToAddressListString(), 
					m.MainEntity.To.ToAddressListString(), m.MainEntity.Date, ms, attachments, exception.Message);
				DownloadLogEntity.Log((ulong)PriceSourceType.EMail, String.Format("Письмо не распознано.Причина : {0}; Тема :{1}; От : {2}", 
					exception.Message, m.MainEntity.Subject, from.ToAddressListString()));
			}
			catch (Exception exMatch)
			{
				_logger.Error("Не удалось отправить нераспознанное письмо", exMatch);
			}
		}

		protected virtual void ProcessAttachs(Mime m, AddressList from)
		{
			//Один из аттачментов письма совпал с источником, иначе - письмо не распознано
			var matched = false;

			var attachmentFileName = string.Empty;

			var attachments = m.GetValidAttachements();
			foreach (var entity in attachments)
			{
				attachmentFileName = SaveAttachement(entity);
				UnPack(m, ref matched, from, attachmentFileName);
				Cleanup();
			}

			if (!matched)
				throw new EMailSourceHandlerException("Не найден источник.");
		}

		/// <summary>
		/// Проверяет, что письмо содержит вложения
		/// </summary>
		protected virtual void CheckMime(Mime m)
		{
			if (m.Attachments.Length == 0)
				throw new EMailSourceHandlerException("Письмо не содержит вложений.");
		}

		/// <summary>
		/// Происходит разбор собственно вложения и сверка его с источниками
		/// </summary>
		private void UnPack(Mime m, ref bool Matched, AddressList FromList, string ShortFileName)
		{
			DataRow[] drLS;

			//Раньше не проверялся весь список From, теперь это делается. Туда же добавляется и Sender
			foreach (var mbFrom in FromList.Mailboxes)
			{
				//Раньше не проверялся весь список TO, теперь это делается
				foreach (var mba in m.MainEntity.To.Mailboxes)
				{
					drLS = dtSources.Select(String.Format("({0} = '{1}') and ({2} like '*{3}*')",
						SourcesTableColumns.colEMailTo, GetCorrectEmailAddress(mba.EmailAddress),
						SourcesTableColumns.colEMailFrom, mbFrom.EmailAddress));
					foreach (DataRow drS in drLS)
					{
						if ((drS[SourcesTableColumns.colPriceMask] is String) && 
							!String.IsNullOrEmpty(drS[SourcesTableColumns.colPriceMask].ToString()))
						{
							var priceMask = (string) drS[SourcesTableColumns.colPriceMask];
							var priceMaskIsMatched = FileHelper.CheckMask(ShortFileName, priceMask);
							if (priceMaskIsMatched)
							{
								SetCurrentPriceCode(drS);

								// Пробуем разархивировать
								var CorrectArchive = CheckFile(drS["ArchivePassword"].ToString());

								if (CorrectArchive)
								{
									string ExtrFile = String.Empty;
									if (ProcessPriceFile(CurrFileName, out ExtrFile, (ulong)PriceSourceType.EMail))
									{
										Matched = true;
										LogDownloadedPrice((ulong)PriceSourceType.EMail, Path.GetFileName(CurrFileName), ExtrFile);
									}
									else
									{
										LogDownloaderFail((ulong)PriceSourceType.EMail, "Не удалось обработать файл '" + 
											Path.GetFileName(CurrFileName) + "'", 
											Path.GetFileName(CurrFileName));
									}
								}
								else
								{
									LogDownloaderFail((ulong)PriceSourceType.EMail, "Не удалось распаковать файл '" + 
										Path.GetFileName(CurrFileName) + "'. Файл поврежден", 
										Path.GetFileName(CurrFileName));
								}
								drS.Delete();
							}
						}
					}
					dtSources.AcceptChanges();

				}//foreach (MailboxAddress mba in m.MainEntity.To.Mailboxes)

			}//foreach (MailboxAddress mbFrom in FromList.Mailboxes)
		}

		protected override void CopyToHistory(UInt64 PriceID)
		{
			string HistoryFileName = DownHistoryPath + PriceID + ".eml";
			string SavedFile = DownHandlerPath + PriceID + ".eml";
			try
			{
				_message.ToFile(SavedFile);
				File.Copy(SavedFile, HistoryFileName);
				File.Delete(SavedFile);
			}
			catch { }
		}

    	protected AddressList GetAddressList(Mime m)
		{
			// Заполняем список адресов From
			AddressList FromList = new AddressList();
			bool SenderFound = false;

			// адрес из поля Sender, может быть не установлен
			string senderAddress = null;
			// Если поле установлено и адрес не пустой
			if ((m.MainEntity.Sender != null) && 
				!String.IsNullOrEmpty(m.MainEntity.Sender.EmailAddress))
			{
				// получаем корректный адрес
				senderAddress = GetCorrectEmailAddress(m.MainEntity.Sender.EmailAddress);
				// Если адрес получился некорректным, то сбрасываем значение поля
				if (!IsMailAddress(senderAddress))
					senderAddress = null;
			}
            // Иногда список адресов оказывается пуст - СПАМ
            if (m.MainEntity.From != null)
            {
                foreach (MailboxAddress a in m.MainEntity.From.Mailboxes)
                {
                    //Проверяем, что адрес что-то содержит
					if (!String.IsNullOrEmpty(a.EmailAddress))
                    {
						// получам корректный адрес
						string correctAddress = GetCorrectEmailAddress(a.EmailAddress);
						// Если после всех проверок адрес является EMail-адресом, то добавляем в список
						if (IsMailAddress(correctAddress))
						{
							FromList.Add(new MailboxAddress(correctAddress));
							if (!String.IsNullOrEmpty(senderAddress) && 
								senderAddress.Equals(correctAddress, StringComparison.OrdinalIgnoreCase))
								SenderFound = true;
						}
                    }
                }
            }

			if (!String.IsNullOrEmpty(senderAddress) && !SenderFound)
				FromList.Add(new MailboxAddress(senderAddress));

			// Иногда список адресов оказывается пуст - СПАМ, 
			// в этом случае создаем пустую коллекцию, чтобы все было в порядке
            if (m.MainEntity.To == null)
                m.MainEntity.To = new AddressList();

			return FromList;
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
			if (ArchiveHelper.IsArchive(fileName))
			{
				if (ArchiveHelper.TestArchive(fileName, archivePassword))
				{
					try
					{
						FileHelper.ExtractFromArhive(fileName, tempExtractDir, archivePassword);
						return true;
					}
					catch (ArchiveHelper.ArchiveException)
					{
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

    }

	public static class MimeEntityExtentions
	{
		public static IEnumerable<MimeEntity> GetValidAttachements(this Mime mime)
		{
			return mime.Attachments.Where(m => !String.IsNullOrEmpty(m.GetFilename()) && m.Data != null);
		}

		public static IEnumerable<string> GetAttachmentFilenames(this Mime mime)
		{
			var result = new List<string>();
			var attachments = mime.GetValidAttachements();
			foreach (var entity in attachments)
				result.Add(entity.GetFilename());
			return result;
		}

		public static string GetFilename(this MimeEntity entity)
		{
			if (!String.IsNullOrEmpty(entity.ContentDisposition_FileName))
				return Path.GetFileName(FileHelper.NormalizeFileName(entity.ContentDisposition_FileName));
			if (!String.IsNullOrEmpty(entity.ContentType_Name))
				return Path.GetFileName(FileHelper.NormalizeFileName(entity.ContentType_Name));
			return null;
		}
	}

	public class EMailSourceHandlerException : Exception
	{
		public EMailSourceHandlerException(string message) : base(message)
		{}

		public EMailSourceHandlerException(string message, string subject, string body) : base(message)
		{
			Body = body;
			Subject = subject;
		}

		public string Body { get; set; }
		public string Subject { get; set; }
	}
}
