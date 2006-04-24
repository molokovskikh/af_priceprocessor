using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.IO;

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
                    GetFileFromSource();
                    bool CorrectArchive = true;
                    //�������� �� ��������� ���� ����������, ���� ���, �� ������������ �� �����
                    if (ArchiveHlp.IsArchive(CurrFileName) && !ArchiveHlp.TestArchive(CurrFileName))
                    {
                        CorrectArchive = false; ;
                    }
                    DataRow[] dr = GetLikeSources();
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
