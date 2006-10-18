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
		//������ ��������� UID, �� ������� �� ���� ��� ��� ���������� ������
		protected List<int> errorUIDs;

		public EMAILSourceHandler(string sourceType)
            : base(sourceType)
        {
			errorUIDs = new List<int>();
		}

        protected override void ProcessData()
        {
            try
            {
                using (IMAP_Client c = new IMAP_Client())
                {
                    c.Connect(Settings.Default.IMAPHost, 143);
					IMAPAuth(c);
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
							sequence_set.Parse("1:*", long.MaxValue);
							items = c.FetchMessages(sequence_set, IMAP_FetchItem_Flags.UID, false, false);
							Ping();
                        }
                        catch (Exception ex)
                        {
                            LoggingToService("On Fetch : " + ex.ToString());
                        }

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
                                    FormLog.Log(this.GetType().Name, "On Parse : " + ex.ToString());
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
                                        FormLog.Log(this.GetType().Name, "On Process : " + ex.ToString());
                                    }
								}

                            }//foreach (IMAP_FetchItem) 

                        }//(items != null) && (items.Length > 0)

                        //���������� �������� �����
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
                    c.Disconnect();
                }
            }
            catch(Exception ex)
            {
                LoggingToService(ex.ToString());
            }
        }

		protected virtual void IMAPAuth(IMAP_Client c)
		{
			c.Authenticate(Settings.Default.IMAPUser, Settings.Default.IMAPPass);
		}

        private void ProcessMime(Mime m)
        {
            //���� �� ����������� ������ ������ � ����������, ����� - ������ �� ����������
            bool Matched = false;

            AddressList FromList = GetAddressList(m);

            string ShortFileName = string.Empty;

            //�������� �����������
            string AttachNames = String.Empty;


            //���� ��� ��������, � ������ �������� ��� UUE, �� ��������� ��� ��� ��������
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

			if (CheckMime(m))
            {
                FillSourcesTable();

				ProcessAttachs(m, ref Matched, FromList, ref AttachNames);
            }

            if (!Matched)
            {
                try
                {
                    MemoryStream ms = new MemoryStream(m.ToByteData());
                    FailMailSend(m.MainEntity.Subject, FromList.ToAddressListString(), m.MainEntity.To.ToAddressListString(), m.MainEntity.Date, ms, AttachNames);
                    Logging(String.Format("������ �� ����������. ���� :{0}; �� : {1}", m.MainEntity.Subject, FromList.ToAddressListString()));
                }
                catch (Exception exMatch)
                {
                    FormLog.Log(this.GetType().Name, "�� ������� ��������� �������������� ������ : " + exMatch.ToString());
                }
            }
        }

		protected virtual void ProcessAttachs(Mime m, ref bool Matched, AddressList FromList, ref string AttachNames)
		{
			bool CorrectArchive = true;
			string ShortFileName = string.Empty;

			foreach (MimeEntity ent in m.Attachments)
			{
				if (!String.IsNullOrEmpty(ent.ContentDisposition_FileName) || !String.IsNullOrEmpty(ent.ContentType_Name))
				{
					ShortFileName = SaveAttachement(ent);
					AttachNames += "\"" + ShortFileName + "\"" + Environment.NewLine;
					CorrectArchive = CheckFile();
					UnPack(m, ref Matched, FromList, ShortFileName, CorrectArchive);
					DeleteCurrFile();
				}
			}
		}

		/// <summary>
		/// ���������, ��� ������ �������� 
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		protected virtual bool CheckMime(Mime m)
		{
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
		/// ������� ��������� ���� ������ � ������� UUE.
		/// </summary>
		/// <param name="m">Mime ������� ������</param>
		/// <returns>��� �������������� �����</returns>
		private string ExtractFromUUE(Mime m)
		{
			//������� ������������� ������� � koi8r -> UTF7 -> default(cp1251)
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
                if (ArchiveHlp.TestArchive(UUEFileName))
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
                    catch (ArchiveHlp.ArchiveException)
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

		protected virtual void UnPack(Mime m, ref bool Matched, AddressList FromList, string ShortFileName, bool CorrectArchive)
		{
			DataRow[] drLS = null;

			//������ �� ���������� ���� ������ From, ������ ��� ��������. ���� �� ����������� � Sender
			foreach (MailboxAddress mbFrom in FromList.Mailboxes)
			{
				//������ �� ���������� ���� ������ TO, ������ ��� ��������
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
									Logging(CurrPriceCode, "�� ������� ���������� ���� '" + Path.GetFileName(CurrFileName) + "'");
								}
							}
							else
							{
								Logging(CurrPriceCode, "�� ������� ����������� ���� '" + Path.GetFileName(CurrFileName) + "'");
							}
							drS.Delete();
						}
					}
					dtSources.AcceptChanges();

				}//foreach (MailboxAddress mba in m.MainEntity.To.Mailboxes)

			}//foreach (MailboxAddress mbFrom in FromList.Mailboxes)
		}

		private AddressList GetAddressList(Mime m)
		{
			//��������� ������ ������� From
			AddressList FromList = new AddressList();
			bool SenderFound = false;

            //������ ������ ������� ����������� ���� - ����
            if (m.MainEntity.From != null)
            {
                foreach (MailboxAddress a in m.MainEntity.From.Mailboxes)
                {
                    //���������, ��� ����� ���-�� ��������
                    if (!String.IsNullOrEmpty(a.EmailAddress))
                    {
                        FromList.Add(new MailboxAddress(a.EmailAddress));
                        if ((m.MainEntity.Sender != null) && (!String.IsNullOrEmpty(m.MainEntity.Sender.EmailAddress)) && (String.Compare(a.EmailAddress, m.MainEntity.Sender.EmailAddress, true) == 0))
                            SenderFound = true;
                    }
                }
            }
			if ((m.MainEntity.Sender != null) && (!String.IsNullOrEmpty(m.MainEntity.Sender.EmailAddress)) && (!SenderFound))
				FromList.Add(new MailboxAddress(m.MainEntity.Sender.EmailAddress));

            //������ ������ ������� ����������� ���� - ����, � ���� ������ ������� ������ ���������, ����� ��� ���� � �������
            if (m.MainEntity.To == null)
                m.MainEntity.To = new AddressList();

			return FromList;
		}

		protected bool CheckFile()
		{
			//�������� �� ��������� ���� ����������, ���� ���, �� ������������ �� �����
			if (ArchiveHlp.IsArchive(CurrFileName))
			{
				if (ArchiveHlp.TestArchive(CurrFileName))
				{
					try
					{
                        ExtractFromArhive(CurrFileName, CurrFileName + ExtrDirSuffix);
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

		protected string SaveAttachement(MimeEntity ent)
		{
			string ShortFileName = String.Empty;
			//� ��������� ������� ContentDisposition_FileName �� ���������, ����� ������� �� ContentType_Name
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
