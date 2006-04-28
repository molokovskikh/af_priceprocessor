using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.Mime;
using Inforoom.Downloader.Properties;
using Inforoom.Formalizer;
using LumiSoft.Net.IMAP;
using System.Text.RegularExpressions;


namespace Inforoom.Downloader
{
    public class EMAILSourceHandler : BaseSourceHandler
    {
        public EMAILSourceHandler(string sourceType)
            : base(sourceType)
        { }

        protected override void ProcessData()
        {
            //набор строк похожих источников
            DataRow[] drLS;
            try
            {
                using (IMAP_Client c = new IMAP_Client())
                {
                    c.Connect(Settings.Default.IMAPHost, 143);
                    c.Authenticate(Settings.Default.IMAPUser, Settings.Default.IMAPPass);
                    c.SelectFolder("INBOX");

                    IMAP_FetchItem[] items = null;
                    List<string> ProcessedUID = null;
                    do
                    {
                        try
                        {
                            Ping();
                            ProcessedUID = new List<string>();
                            IMAP_SequenceSet sequence_set = new IMAP_SequenceSet();
                            sequence_set.Parse("1:*");
                            items = c.FetchMessages(sequence_set, IMAP_FetchItem_Flags.UID, false, false);
                            Ping();
                        }
                        catch (Exception ex)
                        {
                            FormLog.Log(this.GetType().Name, "On Fetch : " + ex.ToString());
                        }

                        //(c.GetUnseenMessagesCount() > 0)
                        if ((items != null) && (items.Length > 0))
                        {
                            foreach (IMAP_FetchItem item in items)
                            {
                                Mime m = null;
                                IMAP_FetchItem[] OneItem = null;
                                try
                                {
                                    IMAP_SequenceSet sequence_Mess = new IMAP_SequenceSet();
                                    sequence_Mess.Parse(item.UID.ToString());
									OneItem = c.FetchMessages(sequence_Mess, IMAP_FetchItem_Flags.Message, false, true);
                                    m = Mime.Parse(OneItem[0].MessageData);
                                    ProcessedUID.Add(item.UID.ToString());
                                    Ping();
                                }
                                catch (Exception ex)
                                {
                                    m = null;
                                    MemoryStream ms = null;
                                    if ((OneItem != null) && (OneItem.Length > 0) && (OneItem[0].MessageData != null))
                                        ms = new MemoryStream(OneItem[0].MessageData);
                                    ErrorMailSend(item.UID, ex.ToString(), ms);
                                    FormLog.Log(this.GetType().Name, "On Parse : " + ex.ToString());
                                }

                                //Один из аттачментов письма совпал с источником, иначе - письмо не распознано
                                bool Matched = false;

                                if (m != null)
                                {
									AddressList FromList = GetAddressList(m);
									bool CorrectArchive = true;
									string ShortFileName = string.Empty;

									if (m.Attachments.Length > 0)
									{
										foreach (MimeEntity ent in m.Attachments)
										{
											if (!String.IsNullOrEmpty(ent.ContentDisposition_FileName) || !String.IsNullOrEmpty(ent.ContentType_Name))
											{
												ShortFileName = SaveAttachement(ent);
												CorrectArchive = CheckFile();
												drLS = UnPack(m, ref Matched, FromList, ShortFileName, CorrectArchive);
											}
										}
									}
									else
									{
										if (IsUUE(m))
										{
											ShortFileName = ExtractFromUUE(m);
											CurrFileName = DownHandlerPath + ShortFileName;
											CorrectArchive = CheckFile();
											drLS = UnPack(m, ref Matched, FromList, ShortFileName, CorrectArchive);
										}
									}
								}
                                if ((m != null) && !Matched)
                                {
                                    try
                                    {
                                        MemoryStream ms = new MemoryStream(m.ToByteData());
                                        FailMailSend(m.MainEntity.Subject, m.MainEntity.From.ToAddressListString(), m.MainEntity.Date, ms);
                                        Logging(String.Format("Письмо не распознано. Тема :{0}; От : {1}", m.MainEntity.Subject, m.MainEntity.From.ToAddressListString()));
                                    }
                                    catch (Exception exMatch)
                                    {
                                        FormLog.Log(this.GetType().Name, "Не удалось отправить нераспознанное письмо : " + exMatch.ToString());
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
                            sequence_setDelete.Parse(uidseq);
                            c.DeleteMessages(sequence_setDelete, true);
                        }

                    }
                    while ((items != null) && (items.Length > 0));
                    c.Disconnect();
                }
            }
            catch(Exception ex)
            {
                FormLog.Log(this.GetType().Name, ex.ToString());
            }
        }

		private bool IsUUE(Mime m)
		{
			string body = Encoding.GetEncoding("koi8-r").GetString(m.MainEntity.Data);
			Regex reg = new Regex(@"begin\s\d\d\d");
			if (reg.Match(body).Success)
			{
				return true;
			}
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
			FileStream file = new FileStream("MailTemp.uue", FileMode.Create);
			string body = Encoding.GetEncoding("koi8-r").GetString(m.MainEntity.Data);
			file.Write(Encoding.Default.GetBytes(body), 0, m.MainEntity.Data.Length);
			file.Flush();
			file.Close();
			if (ArchiveHlp.TestArchive("MailTemp.uue"))
			{
				try
				{
					ExtractFromArhive("MailTemp.uue", "Extr");
					string[] fileList =  Directory.GetFiles("Extr");
					if (fileList.Length > 0)
					{
						if (File.Exists(DownHandlerPath + Path.GetFileName(fileList[0])))
							File.Delete(DownHandlerPath + Path.GetFileName(fileList[0]));
						File.Move(fileList[0], DownHandlerPath + Path.GetFileName(fileList[0]));
						return Path.GetFileName(fileList[0]);
					}
				}
				catch (ArchiveHlp.ArchiveException)
				{
					
				}
			}
			return "";
		}

		private DataRow[] UnPack(Mime m, ref bool Matched, AddressList FromList, string ShortFileName, bool CorrectArchive)
		{
			DataRow[] drLS = null;

			//Раньше не проверялся весь список From, теперь это делается. Туда же добавляется и Sender
			foreach (MailboxAddress mbFrom in FromList.Mailboxes)
			{
				//Раньше не проверялся весь список TO, теперь это делается
				foreach (MailboxAddress mba in m.MainEntity.To.Mailboxes)
				{
					drLS = dtSources.Select(String.Format("({0} = '{1}') and ({2} like '*{3}*')",
						SourcesTable.colEMailTo, mba.EmailAddress,
						SourcesTable.colEMailFrom, mbFrom.EmailAddress));
					foreach (DataRow drS in drLS)
					{
						if ((WildcardsHlp.IsWildcards((string)drS[SourcesTable.colPriceMask]) && WildcardsHlp.Matched((string)drS[SourcesTable.colPriceMask], ShortFileName)) ||
							(String.Compare(ShortFileName, (string)drS[SourcesTable.colPriceMask], true) == 0))
						{
							Matched = true;
							SetCurrentPriceCode(drS);
							if (CorrectArchive)
							{
								if (ProcessPriceFile(CurrFileName))
								{
									Logging(CurrPriceCode, String.Empty);
									UpdateDB(CurrPriceCode, DateTime.Now);
								}
								else
								{
									Logging(CurrPriceCode, "Failed to process price file " + Path.GetFileName(CurrFileName));
								}
							}
							else
							{
								Logging(CurrPriceCode, "Cant unpack file " + Path.GetFileName(CurrFileName));
							}
							drS.Delete();
						}
					}
					dtSources.AcceptChanges();

				}//foreach (MailboxAddress mba in m.MainEntity.To.Mailboxes)

			}//foreach (MailboxAddress mbFrom in FromList.Mailboxes)
			return drLS;
		}

		private AddressList GetAddressList(Mime m)
		{
			//Заполняем список адресов From
			AddressList FromList = new AddressList();
			bool SenderFound = false;
			foreach (MailboxAddress a in m.MainEntity.From.Mailboxes)
			{
				//Проверяем, что адрес что-то содержит
				if (!String.IsNullOrEmpty(a.EmailAddress))
				{
					FromList.Add(new MailboxAddress(a.EmailAddress));
					if ((m.MainEntity.Sender != null) && (!String.IsNullOrEmpty(m.MainEntity.Sender.EmailAddress)) && (String.Compare(a.EmailAddress, m.MainEntity.Sender.EmailAddress, true) == 0))
						SenderFound = true;
				}
			}
			if ((m.MainEntity.Sender != null) && (!String.IsNullOrEmpty(m.MainEntity.Sender.EmailAddress)) && (!SenderFound))
				FromList.Add(new MailboxAddress(m.MainEntity.Sender.EmailAddress));

			FillSourcesTable();
			return FromList;
		}

		private bool CheckFile()
		{
			//Является ли скачанный файл корректным, если нет, то обрабатывать не будем
			if (ArchiveHlp.IsArchive(CurrFileName))
			{
				if (ArchiveHlp.TestArchive(CurrFileName))
				{
					try
					{
						ExtractFromArhive(CurrFileName, CurrFileName + "Extr");
						return true;
					}
					catch (ArchiveHlp.ArchiveException)
					{
						return false;
					}
				}
				else
					return false;
			}
			return true;
		}

		private string SaveAttachement(MimeEntity ent)
		{
			string ShortFileName = String.Empty;
			//В некоторых случаях ContentDisposition_FileName не заполнено, тогда смотрим на ContentType_Name
			if (!String.IsNullOrEmpty(ent.ContentDisposition_FileName))
				ShortFileName = Path.GetFileName(NormalizeFileName(ent.ContentDisposition_FileName));
			else
				ShortFileName = Path.GetFileName(NormalizeFileName(ent.ContentType_Name));
			CurrFileName = DownHandlerPath + ShortFileName;
			using (FileStream fs = new FileStream(CurrFileName, FileMode.Create))
			{
				ent.DataToStream(fs);
				fs.Close();
			}
			return ShortFileName;
		}

    }
}
