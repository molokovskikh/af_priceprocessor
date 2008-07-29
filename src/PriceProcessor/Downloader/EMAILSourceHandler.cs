using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.Mime;
using Inforoom.PriceProcessor.Properties;
using Inforoom.Formalizer;
using LumiSoft.Net.IMAP;
using System.Text.RegularExpressions;
using Inforoom.Logging;
using Inforoom.Common;
using Inforoom.PriceProcessor;


namespace Inforoom.Downloader
{
    public class EMAILSourceHandler : BaseSourceHandler
    {
		//Список ошибочных UID, по которым не надо еще раз отправлять письма
		protected List<int> errorUIDs;

		//UID текущего обрабатываемого письма
		protected int currentUID;

		public EMAILSourceHandler()
            : base()
        {
			this.sourceType = "EMAIL";
			errorUIDs = new List<int>();
		}

		protected string GetCorrectEmailAddress(string Source)
		{
			return Source.Replace("'", String.Empty).Trim();
		}

		protected bool IsMailAddress(string address)
		{
			try
			{
				System.Net.Mail.MailAddress mailAddress = new System.Net.Mail.MailAddress(address);
				return true;
			}
			catch (FormatException)
			{
				return false;
			}
		}

        protected override void ProcessData()
        {
			using (IMAP_Client c = new IMAP_Client())
			{
				c.Connect(Settings.Default.IMAPHost, 143);
				IMAPAuth(c);
				c.SelectFolder("INBOX");

				try
				{
					IMAP_FetchItem[] items = null;
					List<string> ProcessedUID = null;
					do
					{
						Ping();
						ProcessedUID = new List<string>();
						items = null;
						IMAP_SequenceSet sequence_set = new IMAP_SequenceSet();
						sequence_set.Parse("1:*", long.MaxValue);
						items = c.FetchMessages(sequence_set, IMAP_FetchItem_Flags.UID, false, false);
						Ping();

						if ((items != null) && (items.Length > 0))
						{
							foreach (IMAP_FetchItem item in items)
							{
								Mime m = null;
								IMAP_FetchItem[] OneItem = null;
								try
								{
									IMAP_SequenceSet sequence_Mess = new IMAP_SequenceSet();
									sequence_Mess.Parse(item.UID.ToString(), long.MaxValue);
									OneItem = c.FetchMessages(sequence_Mess, IMAP_FetchItem_Flags.Message, false, true);
									m = Mime.Parse(OneItem[0].MessageData);
									currentUID = item.UID;
									ProcessedUID.Add(item.UID.ToString());
									Ping();
								}
								catch (Exception ex)
								{
									if (!errorUIDs.Contains(item.UID))
									{
										m = null;
										MemoryStream ms = null;
										if ((OneItem != null) && (OneItem.Length > 0) && (OneItem[0].MessageData != null))
											ms = new MemoryStream(OneItem[0].MessageData);
										ErrorMailSend(item.UID, ex.ToString(), ms);
										errorUIDs.Add(item.UID);
									}
									SimpleLog.Log(this.GetType().Name, "On Parse : " + ex.ToString());
								}

								if (m != null)
								{
									try
									{
										ProcessMime(m);
									}
									catch (Exception ex)
									{
										if (ProcessedUID.Contains(item.UID.ToString()))
											ProcessedUID.Remove(item.UID.ToString());
										if (!errorUIDs.Contains(item.UID))
										{
											MemoryStream ms = null;
											if ((OneItem != null) && (OneItem.Length > 0) && (OneItem[0].MessageData != null))
												ms = new MemoryStream(OneItem[0].MessageData);
											ErrorMailSend(item.UID, ex.ToString(), ms);
											errorUIDs.Add(item.UID);
										}
										SimpleLog.Log(this.GetType().Name, "On Process : " + ex.ToString());
									}
								}

							}//foreach (IMAP_FetchItem) 

						}//(items != null) && (items.Length > 0)

						//Производим удаление писем
						if ((items != null) && (items.Length > 0) && (ProcessedUID.Count > 0))
						{
							string uidseq = String.Empty;
							uidseq = String.Join(",", ProcessedUID.ToArray());
							IMAP_SequenceSet sequence_setDelete = new IMAP_SequenceSet();
							sequence_setDelete.Parse(uidseq, long.MaxValue);
							c.DeleteMessages(sequence_setDelete, true);
						}

					}
					while ((items != null) && (items.Length > 0));
				}
				finally
				{
					try { c.Disconnect(); }
					catch { }
				}
			}
        }

		protected virtual void IMAPAuth(IMAP_Client c)
		{
			c.Authenticate(Settings.Default.IMAPUser, Settings.Default.IMAPPass);
		}

        private void ProcessMime(Mime m)
        {
            AddressList FromList = GetAddressList(m);

            string ShortFileName = string.Empty;

            //Название аттачментов
            string AttachNames = String.Empty;
			string _causeSubject = String.Empty, _causeBody = String.Empty, _systemError = String.Empty;


            //Если нет вложений, а письмо выглядит как UUE, то добавляем его как вложение
			if ((m.Attachments.Length == 0) && IsUUE(m))
            {
                ShortFileName = ExtractFromUUE(m);
                if (!String.IsNullOrEmpty(ShortFileName))
                {
                    MimeEntity uueAttach = new MimeEntity();
                    uueAttach.ContentType = MediaType_enum.Application_octet_stream;
                    uueAttach.ContentDisposition = ContentDisposition_enum.Attachment;
                    uueAttach.ContentTransferEncoding = ContentTransferEncoding_enum.Base64;
                    uueAttach.ContentDisposition_FileName = ShortFileName;
                    uueAttach.ContentType_Name = ShortFileName;
                    uueAttach.DataFromFile(DownHandlerPath + ShortFileName);
					if (m.MainEntity.ContentType != LumiSoft.Net.Mime.MediaType_enum.Multipart_mixed)
					{
						m.MainEntity.Data = null;
						m.MainEntity.ContentType = LumiSoft.Net.Mime.MediaType_enum.Multipart_mixed;
					}
					m.MainEntity.ChildEntities.Add(uueAttach);
                }
            }

			//Формируем список приложений, чтобы использовать его при отчете о нераспознанном письме
			foreach (MimeEntity ent in m.Attachments)
				if (!String.IsNullOrEmpty(ent.ContentDisposition_FileName) || !String.IsNullOrEmpty(ent.ContentType_Name))
					AttachNames += "\"" + GetShortFileNameFromAttachement(ent) + "\"" + Environment.NewLine;


			if (CheckMime(m, ref _causeSubject, ref _causeBody, ref _systemError))
			{
				FillSourcesTable();

				if (!ProcessAttachs(m, FromList, ref _causeSubject, ref _causeBody))
					ErrorOnProcessAttachs(m, FromList, AttachNames, _causeSubject, _causeBody);
			}
			else
				ErrorOnCheckMime(m, FromList, AttachNames, _causeSubject, _causeBody, _systemError);
        }

		protected virtual void ErrorOnProcessAttachs(Mime m, AddressList FromList, string AttachNames, string causeSubject, string causeBody)
		{
			SendUnrecLetter(m, FromList, AttachNames, causeBody);
		}

		protected virtual void ErrorOnCheckMime(Mime m, AddressList FromList, string AttachNames, string causeSubject, string causeBody, string systemError)
		{
			SendUnrecLetter(m, FromList, AttachNames, causeBody);
		}

		protected virtual void SendUnrecLetter(Mime m, AddressList FromList, string AttachNames, string cause)
		{
			try
			{
				MemoryStream ms = new MemoryStream(m.ToByteData());
				try
				{
					LumiSoft.Net.SMTP.Client.SmtpClientEx.QuickSendSmartHost(
						Settings.Default.SMTPHost,
						25,
						Environment.MachineName,
						Settings.Default.ServiceMail,
						new string[] { Settings.Default.UnrecLetterMail },
						ms);
				}
				catch { }
				FailMailSend(m.MainEntity.Subject, FromList.ToAddressListString(), m.MainEntity.To.ToAddressListString(), m.MainEntity.Date, ms, AttachNames, cause);
				Logging(String.Format("Письмо не распознано.Причина : {0}; Тема :{1}; От : {2}", cause, m.MainEntity.Subject, FromList.ToAddressListString()));
			}
			catch (Exception exMatch)
			{
				SimpleLog.Log(this.GetType().Name, "Не удалось отправить нераспознанное письмо : " + exMatch.ToString());
			}
		}

		protected virtual bool ProcessAttachs(Mime m, AddressList FromList, ref string causeSubject, ref string causeBody)
		{
			//Один из аттачментов письма совпал с источником, иначе - письмо не распознано
			bool _Matched = false;

			bool CorrectArchive = true;
			string ShortFileName = string.Empty;

			foreach (MimeEntity ent in m.Attachments)
			{
				if (!String.IsNullOrEmpty(ent.ContentDisposition_FileName) || !String.IsNullOrEmpty(ent.ContentType_Name))
				{
					ShortFileName = SaveAttachement(ent);
					CorrectArchive = CheckFile();
					UnPack(m, ref _Matched, FromList, ShortFileName, CorrectArchive);
					DeleteCurrFile();
				}
			}

			if (!_Matched)
				causeBody = "Не найден источник.";
			return _Matched;
		}

		/// <summary>
		/// Проверяет, что письмо содержит 
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		protected virtual bool CheckMime(Mime m, ref string causeSubject, ref string causeBody, ref string systemError)
		{
			if (m.Attachments.Length == 0)
			{
				causeBody = "Письмо не содержит вложений.";
				systemError = causeBody;
			}
			return m.Attachments.Length > 0;
		}

		private bool IsUUE(Mime m)
		{
            if (m.MainEntity.Data != null)
            {
                string body = Encoding.GetEncoding("koi8-r").GetString(m.MainEntity.Data);
				Regex reg = new Regex(@"(.*?\r\n\r\n)?begin\s\d\d\d");
                return reg.Match(body).Success;
            }
            else
                return false;
		}

		/// <summary>
		/// Функция обработки тела письма в формате UUE.
		/// </summary>
		/// <param name="m">Mime элемент письма</param>
		/// <returns>Имя распакованного файла</returns>
		private string ExtractFromUUE(Mime m)
		{
			//Двойная перекодировка сначала и koi8r -> UTF7 -> default(cp1251)
            string UUEFileName = DownHandlerPath + "MailTemp.uue";
            using (FileStream file = new FileStream(UUEFileName, FileMode.Create))
            {
                string body = Encoding.GetEncoding("koi8-r").GetString(m.MainEntity.Data);
				int Index = body.IndexOf("begin ");
				body = body.Substring(Index);
				file.Write(Encoding.Default.GetBytes(body), 0, Encoding.Default.GetByteCount(body));
                file.Flush();
                file.Close();
            }
            try
            {
                if (ArchiveHelper.TestArchive(UUEFileName))
                {
                    try
                    {
                        ExtractFromArhive(UUEFileName, UUEFileName + ExtrDirSuffix);
                        string[] fileList = Directory.GetFiles(UUEFileName + ExtrDirSuffix);
                        if (fileList.Length > 0)
                        {
                            if (File.Exists(DownHandlerPath + Path.GetFileName(fileList[0])))
                                File.Delete(DownHandlerPath + Path.GetFileName(fileList[0]));
                            File.Move(fileList[0], DownHandlerPath + Path.GetFileName(fileList[0]));
                            return Path.GetFileName(fileList[0]);
                        }
                    }
                    catch (ArchiveHelper.ArchiveException)
                    {

                    }
                }
            }
            finally
            {
                if (Directory.Exists(UUEFileName + ExtrDirSuffix))
                    try
                    {
                        Directory.Delete(UUEFileName + ExtrDirSuffix, true);
                    }
                    catch { }
            } 
            return String.Empty;
		}

		/// <summary>
		/// Происходит разбор собственно вложения и сверка его с источниками
		/// </summary>
		/// <param name="m"></param>
		/// <param name="Matched"></param>
		/// <param name="FromList"></param>
		/// <param name="ShortFileName"></param>
		/// <param name="CorrectArchive"></param>
		protected virtual void UnPack(Mime m, ref bool Matched, AddressList FromList, string ShortFileName, bool CorrectArchive)
		{
			DataRow[] drLS = null;

			//Раньше не проверялся весь список From, теперь это делается. Туда же добавляется и Sender
			foreach (MailboxAddress mbFrom in FromList.Mailboxes)
			{
				//Раньше не проверялся весь список TO, теперь это делается
				foreach (MailboxAddress mba in m.MainEntity.To.Mailboxes)
				{
					drLS = dtSources.Select(String.Format("({0} = '{1}') and ({2} like '*{3}*')",
						SourcesTable.colEMailTo, GetCorrectEmailAddress(mba.EmailAddress),
						SourcesTable.colEMailFrom, mbFrom.EmailAddress));
					foreach (DataRow drS in drLS)
					{
						if ((drS[SourcesTable.colPriceMask] is String) && !String.IsNullOrEmpty(drS[SourcesTable.colPriceMask].ToString()))
						{
							if ((WildcardsHelper.IsWildcards((string)drS[SourcesTable.colPriceMask]) && WildcardsHelper.Matched((string)drS[SourcesTable.colPriceMask], ShortFileName)) ||
								(String.Compare(ShortFileName, (string)drS[SourcesTable.colPriceMask], true) == 0))
							{
								SetCurrentPriceCode(drS);
								if (CorrectArchive)
								{
									string ExtrFile = String.Empty;
									if (ProcessPriceFile(CurrFileName, out ExtrFile))
									{
										Matched = true;
										LogDownloaderPrice(m, null, DownPriceResultCode.SuccessDownload, Path.GetFileName(CurrFileName), ExtrFile);
									}
									else
									{
										LogDownloaderPrice(m, "Не удалось обработать файл '" + Path.GetFileName(CurrFileName) + "'", DownPriceResultCode.ErrorProcess, Path.GetFileName(CurrFileName), null);
									}
								}
								else
								{
									LogDownloaderPrice(m, "Не удалось распаковать файл '" + Path.GetFileName(CurrFileName) + "'", DownPriceResultCode.ErrorProcess, Path.GetFileName(CurrFileName), null);
								}
								drS.Delete();
							}
						}
					}
					dtSources.AcceptChanges();

				}//foreach (MailboxAddress mba in m.MainEntity.To.Mailboxes)

			}//foreach (MailboxAddress mbFrom in FromList.Mailboxes)
		}

		private void LogDownloaderPrice(Mime Letter, string AdditionMessage, DownPriceResultCode resultCode, string ArchFileName, string ExtrFileName)
		{
			ulong PriceID = Logging(CurrPriceItemId, AdditionMessage, resultCode, ArchFileName, (String.IsNullOrEmpty(ExtrFileName)) ? null : Path.GetFileName(ExtrFileName));
			if (PriceID != 0)
			{
				CopyToHistory(PriceID, Letter);

				//Если все сложилось, то копируем файл в Inbound
				if (resultCode == DownPriceResultCode.SuccessDownload)
				{
					string NormalName = FileHelper.NormalizeDir(Settings.Default.InboundPath) + "d" + CurrPriceItemId.ToString() + "_" + PriceID.ToString() + GetExt();
					try
					{
						if (File.Exists(NormalName))
							File.Delete(NormalName);
						File.Copy(ExtrFileName, NormalName);
						SimpleLog.Log(this.GetType().Name + "." + CurrPriceItemId.ToString(), "Попытка добавить прайс-листа " + (string)drCurrent[SourcesTable.colShortName] + " - " + (string)drCurrent[SourcesTable.colPriceName] + " в PriceItemList");
						PriceProcessItem item = new PriceProcessItem(true, Convert.ToUInt64(CurrPriceCode), CurrCostCode, CurrPriceItemId, NormalName);
						item.FileTime = DateTime.Now;
						PriceItemList.AddItem(item);
						SimpleLog.Log(this.GetType().Name + "." + CurrPriceItemId.ToString(), "Price " + (string)drCurrent[SourcesTable.colShortName] + " - " + (string)drCurrent[SourcesTable.colPriceName] + " скачан/распакован");
					}
					catch (Exception ex)
					{
						//todo: по идее здесь не должно возникнуть ошибок, но на всякий случай логируем, возможно надо включить логирование письмом
						SimpleLog.Log(this.GetType().Name + CurrPriceItemId.ToString(), String.Format("Не удалось перенести файл '{0}' в каталог '{1}': {2} ", ExtrFileName, NormalName, ex));
					}
				}
			}
			else
				throw new Exception(String.Format("При логировании прайс-листа {0} получили 0 значение в ID;", CurrPriceItemId));
		}

		void CopyToHistory(UInt64 PriceID, Mime Letter)
		{
			string HistoryFileName = DownHistoryPath + PriceID.ToString() + ".eml";
			string SavedFile = DownHandlerPath + PriceID.ToString() + ".eml";
			try
			{
				Letter.ToFile(SavedFile);
				File.Copy(SavedFile, HistoryFileName);
				File.Delete(SavedFile);
			}
			catch { }
		}

		protected AddressList GetAddressList(Mime m)
		{
			//Заполняем список адресов From
			AddressList FromList = new AddressList();
			bool SenderFound = false;

			//адрес из поля Sender, может быть не установлен
			string senderAddress = null;
			//Если поле установлено и адрес не пустой
			if ((m.MainEntity.Sender != null) && !String.IsNullOrEmpty(m.MainEntity.Sender.EmailAddress))
			{ 
				//получаем корректный адрес
				senderAddress = GetCorrectEmailAddress(m.MainEntity.Sender.EmailAddress);
				//Если адрес получился некорректным, то сбрасываем значение поля
				if (!IsMailAddress(senderAddress))
					senderAddress = null;
			}

            //Иногда список адресов оказывается пуст - СПАМ
            if (m.MainEntity.From != null)
            {
                foreach (MailboxAddress a in m.MainEntity.From.Mailboxes)
                {
                    //Проверяем, что адрес что-то содержит
					if (!String.IsNullOrEmpty(a.EmailAddress))
                    {
						//получам корректный адрес
						string correctAddress = GetCorrectEmailAddress(a.EmailAddress);
						//Если после всех проверок адрес является EMail-адресом, то добавляем в список
						if (IsMailAddress(correctAddress))
						{
							FromList.Add(new MailboxAddress(correctAddress));
							if (!String.IsNullOrEmpty(senderAddress) && senderAddress.Equals(correctAddress, StringComparison.OrdinalIgnoreCase))
								SenderFound = true;
						}
                    }
                }
            }

			if (!String.IsNullOrEmpty(senderAddress) && !SenderFound)
				FromList.Add(new MailboxAddress(senderAddress));

			//Иногда список адресов оказывается пуст - СПАМ, в этом случае создаем пустую коллекцию, чтобы все было в порядке
            if (m.MainEntity.To == null)
                m.MainEntity.To = new AddressList();

			return FromList;
		}

		protected bool CheckFile()
		{
			//Является ли скачанный файл корректным, если нет, то обрабатывать не будем
			if (ArchiveHelper.IsArchive(CurrFileName))
			{
				if (ArchiveHelper.TestArchive(CurrFileName))
				{
					try
					{
                        ExtractFromArhive(CurrFileName, CurrFileName + ExtrDirSuffix);
						return true;
					}
					catch (ArchiveHelper.ArchiveException)
					{
						return false;
					}
				}
				else
					return false;
			}
			return true;
		}

		protected string SaveAttachement(MimeEntity ent)
		{
			string ShortFileName = GetShortFileNameFromAttachement(ent);
			CurrFileName = DownHandlerPath + ShortFileName;
			using (FileStream fs = new FileStream(CurrFileName, FileMode.Create))
			{
				ent.DataToStream(fs);
				fs.Close();
			}
			return ShortFileName;
		}

		protected string GetShortFileNameFromAttachement(MimeEntity ent)
		{
			string ShortFileName = String.Empty;
			//В некоторых случаях ContentDisposition_FileName не заполнено, тогда смотрим на ContentType_Name
			if (!String.IsNullOrEmpty(ent.ContentDisposition_FileName))
				ShortFileName = Path.GetFileName(NormalizeFileName(ent.ContentDisposition_FileName));
			else
				ShortFileName = Path.GetFileName(NormalizeFileName(ent.ContentType_Name));
			return ShortFileName;
		}

    }
}
