using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.IO;
using Inforoom.Formalizer;

namespace Inforoom.Downloader
{
    public abstract class PathSourceHandler : BaseSourceHandler
    {
        public PathSourceHandler(string sourceType)
            : base(sourceType)
        { 
        }

        protected override void ProcessData()
        {
            //набор строк похожих источников
            DataRow[] drLS;
            FillSourcesTable();
            while (dtSources.Rows.Count > 0)
            {
                drLS = null;
                try
                {
                    drLS = GetLikeSources();
#if DEBUG
                    if (drLS.Length < 1)
                        FormLog.Log(this.GetType().Name, "!!!!!!!!!!!!!   drLS.Length < 1");
#endif
                    GetFileFromSource();

                    //FormLog.Log(SourceType == "LAN", this.GetType().Name, "Обработали источник с кодом {0} файл '{1}'", dtSources.Rows[0][SourcesTable.colPriceCode], CurrFileName);

                    if (!String.IsNullOrEmpty(CurrFileName))
                    {
                        bool CorrectArchive = true;
                        //Является ли скачанный файл корректным, если нет, то обрабатывать не будем
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
                        foreach (DataRow drS in drLS)
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
                    else
                    {
                        foreach (DataRow drDel in drLS)
                            drDel.Delete();
                    }
                    dtSources.AcceptChanges();
                }
                catch(Exception ex)
                {
                    string Error = String.Empty;
                    if ((drLS != null) && (drLS.Length > 1))
                    {
                        foreach (DataRow drS in drLS)
                        {
                            if (Error == String.Empty)
                                Error += drS[SourcesTable.colPriceCode].ToString();
                            else
                                Error += ", " + drS[SourcesTable.colPriceCode].ToString();
                            try
                            {
                                drS.Delete();
                            }
                            catch { }
                        }
                        Error = "Источники : " + Error;
                    }
                    else
                    {
                        Error = String.Format("Источник : {0}", dtSources.Rows[0][SourcesTable.colPriceCode]);
                        try
                        {
                            dtSources.Rows[0].Delete();
                        }
                        catch { }
                    }
                    Error += Environment.NewLine + Environment.NewLine + ex.ToString();
                    LoggingToService(Error);
                    try
                    {
                        dtSources.AcceptChanges();
                    }
                    catch { }
                }
            }
        }

        protected void CopyStreams(Stream input, Stream output)
        {
            const int size = 4096;
            byte[] bytes = new byte[4096];
            int numBytes;
            while ((numBytes = input.Read(bytes, 0, size)) > 0)
                output.Write(bytes, 0, numBytes);
        }

        /// <summary>
        /// Получает файл из источника, взятого из таблицы первым
        /// </summary>
        protected abstract void GetFileFromSource();

        /// <summary>
        /// Получить прайс-листы, у которых истоники совпадают с первым в списке
        /// </summary>
        /// <returns></returns>
        protected abstract DataRow[] GetLikeSources();
    }
}
