using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.Mime;
using Inforoom.Downloader.Properties;
using Inforoom.Formalizer;


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

                    IMAP_FetchItem[] items = c.FetchMessages(1, -1, false, true, false);

                    if (c.GetUnseenMessagesCount() > 0)
                    {
                        foreach (IMAP_FetchItem item in items)
                            if (item.IsNewMessage)
                            {
                                Mime m = Mime.Parse(item.Data);
                                if (m.Attachments.Length > 0)
                                {
                                    FillSourcesTable();
                                    foreach (MimeEntity ent in m.Attachments)
                                        if (ent.ContentType == MediaType_enum.Application_octet_stream)
                                        {
                                            
                                            string ShortFileName = Path.GetFileName(ent.ContentDisposition_FileName);
                                            CurrFileName = DownHandlerPath + Path.GetFileName(ent.ContentDisposition_FileName);
                                            using (FileStream fs = new FileStream(CurrFileName, FileMode.CreateNew))
                                            {
                                                ent.ToStream(fs);
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
                                                SourcesTable.colEMailTo, dtSources.Rows[0][SourcesTable.colEMailTo],
                                                SourcesTable.colEMailFrom, dtSources.Rows[0][SourcesTable.colEMailFrom]));
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
                                                            UpdateDB(CurrPriceCode, CurrPriceDate);
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
                    c.DeleteMessages(1, -1, false);
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
