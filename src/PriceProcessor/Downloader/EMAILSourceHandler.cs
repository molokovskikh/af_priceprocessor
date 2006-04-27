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


namespace Inforoom.Downloader
{
    public class EMAILSourceHandler : BaseSourceHandler
    {
        public EMAILSourceHandler(string sourceType)
            : base(sourceType)
        { }

        protected override void ProcessData()
        {
            //����� ����� ������� ����������
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

                                //���� �� ����������� ������ ������ � ����������, ����� - ������ �� ����������
                                bool Matched = false;

                                if ((m != null) && (m.Attachments.Length > 0))
                                {
                                    FillSourcesTable();
                                    foreach (MimeEntity ent in m.Attachments)
                                        if (!String.IsNullOrEmpty(ent.ContentDisposition_FileName))
                                        {
                                            string ShortFileName = Path.GetFileName(ent.ContentDisposition_FileName);
                                            CurrFileName = DownHandlerPath + Path.GetFileName(ent.ContentDisposition_FileName);
                                            using (FileStream fs = new FileStream(CurrFileName, FileMode.Create))
                                            {
                                                ent.DataToStream(fs);
                                                fs.Close();
                                            }
                                            bool CorrectArchive = true;
                                            //�������� �� ��������� ���� ����������, ���� ���, �� ������������ �� �����
                                            if (ArchiveHlp.IsArchive(CurrFileName))
                                            {
                                                if (ArchiveHlp.TestArchive(CurrFileName))
                                                {
                                                    try
                                                    {
                                                        ExtractFromArhive(CurrFileName, CurrFileName + "Extr");
                                                    }
                                                    catch (ArchiveHlp.ArchiveException)
                                                    {
                                                        CorrectArchive = false;
                                                    }
                                                }
                                                else
                                                    CorrectArchive = false;
                                            }

                                            drLS = dtSources.Select(String.Format("({0} = '{1}') and ({2} like '*{3}*')",
                                                SourcesTable.colEMailTo, m.MainEntity.To.Mailboxes[0].EmailAddress,
                                                SourcesTable.colEMailFrom, m.MainEntity.From.Mailboxes[0].EmailAddress));
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

                                        }//if (ent.FileName != String.Empty)

                                }// (m != null) && (m.Attachments.Length > 0)

                                if ((m != null) && !Matched)
                                {
                                    try
                                    {
                                        MemoryStream ms = new MemoryStream(m.ToByteData());
                                        FailMailSend(m.MainEntity.Subject, m.MainEntity.From.Mailboxes[0].EmailAddress, m.MainEntity.Date, ms);
                                        Logging(String.Format("������ �� ����������. ���� :{0}; �� : {1}", m.MainEntity.Subject, m.MainEntity.From.Mailboxes[0].EmailAddress));
                                    }
                                    catch (Exception exMatch)
                                    {
                                        FormLog.Log(this.GetType().Name, "�� ������� ��������� �������������� ������ : " + exMatch.ToString());
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


    }
}
