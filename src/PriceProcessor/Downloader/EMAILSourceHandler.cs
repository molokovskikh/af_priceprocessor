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
                    do
                    {
                        try
                        {
                            IMAP_SequenceSet sequence_set = new IMAP_SequenceSet();
                            sequence_set.Parse("1:5");
                            items = c.FetchMessages(sequence_set, IMAP_FetchItem_Flags.All, false, false);
                        }
                        catch (Exception ex)
                        {
                            FormLog.Log(this.GetType().Name, "On Fetch : " + ex.ToString());
                        }

                        if ((c.GetUnseenMessagesCount() > 0) && (items != null))
                        {
                            foreach (IMAP_FetchItem item in items)
                                if (item.IsNewMessage)
                                {
                                    Mime m = null;
                                    try
                                    {
                                        m = Mime.Parse(item.MessageData);
                                    }
                                    catch (Exception ex)
                                    {
                                        m = null;
                                        FormLog.Log(this.GetType().Name, "On Parse : " + ex.ToString());
                                    }
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
                                                //явл€етс€ ли скачанный файл корректным, если нет, то обрабатывать не будем
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
                                                        (ShortFileName.ToLower() == ((string)drS[SourcesTable.colPriceMask]).ToLower()))
                                                    {
                                                        SetCurrentPriceCode(drS);
                                                        OperatorMailSend();
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

                                    }
                                }//if(item.IsNewMessage) 
                        }//else
                        if (items != null && items.Length > 0)
                        {
                            string uidseq = items[0].UID.ToString();
                            for (int i = 1; i < items.Length; i++)
                            {
                                uidseq += "," + items[i].UID.ToString();
                            }
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
