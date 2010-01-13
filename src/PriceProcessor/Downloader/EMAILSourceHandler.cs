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
							}
							catch (Exception ex)
							{
								_logger.Error("On Parse", ex);
								_message = null;
								SendBrokenMessage(item, OneItem, ex);
							}

							if (_message == null)
								continue;

							try
							{
								ProcessMime(_message);
							}
							catch (Exception ex)
							{
								_logger.Error("On Process", ex);
								SendBrokenMessage(item, OneItem, ex);
							}
						}//foreach (IMAP_FetchItem)

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
            var FromList = GetAddressList(m);

            // Название аттачментов
            string AttachNames = String.Empty;
			string _causeSubject = String.Empty, _causeBody = String.Empty, _systemError = String.Empty;

			m = UueHelper.ExtractFromUue(m, DownHandlerPath);			
        	try
        	{
        		if (!CheckMime(m, ref _causeSubject, ref _causeBody, ref _systemError))
        			throw new EMailSourceHandlerException();
        		FillSourcesTable();
        		if (!ProcessAttachs(m, FromList, ref _causeSubject, ref _causeBody))
        			throw new EMailSourceHandlerException();
        	}
        	catch (EMailSourceHandlerException)
        	{
				// Формируем список приложений, чтобы использовать 
				// его при отчете о нераспознанном письме
        		AttachNames = m.Attachments.Where(a => !String.IsNullOrEmpty(a.GetFilename())).Aggregate("", (s, a) => s + String.Format("\"{0}\"\r\n", a.GetFilename()));
        		ErrorOnCheckMime(m, FromList, AttachNames, _causeSubject, _causeBody, _systemError);
        	}
        }

		protected virtual void ErrorOnProcessAttachs(Mime m, AddressList FromList, string AttachNames, string causeSubject, string causeBody)
		{
			SendUnrecLetter(m, FromList, AttachNames, causeBody);
		}

		protected virtual void ErrorOnCheckMime(Mime m, AddressList FromList, string AttachNames, string causeSubject, string causeBody, string systemError)
		{
			SendUnrecLetter(m, FromList, AttachNames, causeBody);
		}

		protected virtual void SendUnrecLetter(Mime m, AddressList FromList, 
			string AttachNames, string cause)
		{
			try
			{
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
				FailMailSend(m.MainEntity.Subject, FromList.ToAddressListString(), 
					m.MainEntity.To.ToAddressListString(), m.MainEntity.Date, ms, AttachNames, cause);
				DownloadLogEntity.Log(String.Format("Письмо не распознано.Причина : {0}; Тема :{1}; От : {2}", 
					cause, m.MainEntity.Subject, FromList.ToAddressListString()));
			}
			catch (Exception exMatch)
			{
				_logger.Error("Не удалось отправить нераспознанное письмо", exMatch);
			}
		}

		protected virtual bool ProcessAttachs(Mime m, AddressList FromList, 
			ref string causeSubject, ref string causeBody)
		{
			//Один из аттачментов письма совпал с источником, иначе - письмо не распознано
			bool _Matched = false;

			var attachmentFileName = string.Empty;

			var attachments = m.GetValidAttachements();
			foreach (var entity in attachments)
			{
				attachmentFileName = SaveAttachement(entity);
				UnPack(m, ref _Matched, FromList, attachmentFileName);
				Cleanup();
			}

			if (!_Matched)
				causeBody = "Не найден источник.";
			return _Matched;
		}

		/// <summary>
		/// Проверяет, что письмо содержит вложения
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		protected virtual bool CheckMime(Mime m, ref string causeSubject, 
			ref string causeBody, ref string systemError)		
		{
			if (m.Attachments.Length == 0)
			{
				causeBody = "Письмо не содержит вложений.";
				systemError = causeBody;
			}
			return m.Attachments.Length > 0;
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
							if ((WildcardsHelper.IsWildcards((string)drS[SourcesTableColumns.colPriceMask]) && 
								WildcardsHelper.Matched((string)drS[SourcesTableColumns.colPriceMask], ShortFileName)) ||
								(String.Compare(ShortFileName, (string)drS[SourcesTableColumns.colPriceMask], true) == 0))
							{
								SetCurrentPriceCode(drS);

								// Пробуем разархивировать
								var CorrectArchive = CheckFile(drS["ArchivePassword"].ToString());

								if (CorrectArchive)
								{
									string ExtrFile = String.Empty;
									if (ProcessPriceFile(CurrFileName, out ExtrFile))
									{
										Matched = true;
										LogDownloadedPrice(Path.GetFileName(CurrFileName), ExtrFile);
									}
									else
									{
										LogDownloaderFail("Не удалось обработать файл '" + 
											Path.GetFileName(CurrFileName) + "'", 
											Path.GetFileName(CurrFileName));
									}
								}
								else
								{
									LogDownloaderFail("Не удалось распаковать файл '" + 
										Path.GetFileName(CurrFileName) + "'", 
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
			{
				ent.DataToStream(fs);
				fs.Close();
			}
			return attachmentFileName;
		}

		protected static string GetShortFileNameFromAttachement(MimeEntity ent)
		{
			var shortFileName = String.Empty;

			// В некоторых случаях ContentDisposition_FileName не заполнено,
			// тогда смотрим на ContentType_Name
			if (!String.IsNullOrEmpty(ent.ContentDisposition_FileName))
				shortFileName = Path.GetFileName(
					FileHelper.NormalizeFileName(ent.ContentDisposition_FileName));
			else
				shortFileName = Path.GetFileName
					(FileHelper.NormalizeFileName(ent.ContentType_Name));
			return shortFileName;
		}

    }

	public static class MimeEntityExtentions
	{
		public static IEnumerable<MimeEntity> GetValidAttachements(this Mime mime)
		{
			return mime.Attachments.Where(m => !String.IsNullOrEmpty(m.GetFilename()));
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
			else if (!String.IsNullOrEmpty(entity.ContentType_Name))
				return Path.GetFileName(FileHelper.NormalizeFileName(entity.ContentType_Name));
			return null;
		}
	}

	public class EMailSourceHandlerException : Exception
	{
		
	}
}
