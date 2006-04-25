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
            //����� ����� ������� ����������
            DataRow[] drLS;
            FillSourcesTable();
            while (dtSources.Rows.Count > 0)
            {
                drLS = null;
                try
                {
#if DEBUG
                    drLS = GetLikeSources();
                    if (drLS.Length < 1)
                        FormLog.Log(this.GetType().Name, "!!!!!!!!!!!!!   drLS.Length < 1");
#endif
                    GetFileFromSource();

                    if (!String.IsNullOrEmpty(CurrFileName))
                    {
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
                        drLS = GetLikeSources();
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
                        FormLog.Log(this.GetType().Name, "Skip source with pricecode = " + dtSources.Rows[0][SourcesTable.colPriceCode].ToString());
                        dtSources.Rows[0].Delete();
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
                        Error = "��������� : " + Error;
                    }
                    else
                    {
                        Error = String.Format("�������� : {0}", dtSources.Rows[0][SourcesTable.colPriceCode]);
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
        /// �������� ���� �� ���������, ������� �� ������� ������
        /// </summary>
        protected abstract void GetFileFromSource();

        /// <summary>
        /// �������� �����-�����, � ������� �������� ��������� � ������ � ������
        /// </summary>
        /// <returns></returns>
        protected abstract DataRow[] GetLikeSources();
    }
}
