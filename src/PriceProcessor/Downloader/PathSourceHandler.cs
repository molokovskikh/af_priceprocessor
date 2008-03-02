using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.IO;
using Inforoom.Formalizer;
using Inforoom.Logging;

namespace Inforoom.Downloader
{
    public abstract class PathSourceHandler : BaseSourceHandler
    {
        public PathSourceHandler()
            : base()
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
                    drLS = GetLikeSources();
#if DEBUG
                    if (drLS.Length < 1)
                        SimpleLog.Log(this.GetType().Name, "!!!!!!!!!!!!!   drLS.Length < 1");
#endif
                    GetFileFromSource();

                    //SimpleLog.Log(SourceType == "LAN", this.GetType().Name, "���������� �������� � ����� {0} ���� '{1}'", dtSources.Rows[0][SourcesTable.colPriceCode], CurrFileName);

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
                                    ExtractFromArhive(CurrFileName, CurrFileName + ExtrDirSuffix);
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
                            if (CorrectArchive)
                            {
								string ExtrFile = String.Empty;
								if (ProcessPriceFile(CurrFileName, out ExtrFile))
                                {
									LogDownloaderPrice(null, DownPriceResultCode.SuccessDownload, Path.GetFileName(CurrFileName), Path.GetFileName(ExtrFile));
                                    UpdateDB(CurrPriceCode, CurrPriceDate);
                                }
                                else
                                {
									LogDownloaderPrice("�� ������� ���������� ���� '" + Path.GetFileName(CurrFileName) + "'", DownPriceResultCode.ErrorProcess, Path.GetFileName(CurrFileName), null);
                                }
                            }
                            else
                            {
								LogDownloaderPrice("�� ������� ����������� ���� '" + Path.GetFileName(CurrFileName) + "'", DownPriceResultCode.ErrorProcess, Path.GetFileName(CurrFileName), null);
                            }
                            drS.Delete();
                        }
                        DeleteCurrFile();
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

		private void LogDownloaderPrice(string AdditionMessage, DownPriceResultCode resultCode, string ArchFileName, string ExtrFileName)
		{
			ulong PriceID = Logging(CurrPriceCode, AdditionMessage, resultCode, ArchFileName, ExtrFileName);
			if (PriceID != 0)
				CopyToHistory(PriceID, CurrFileName);
			else
				throw new Exception(String.Format("��� ����������� �����-����� {0} �������� 0 �������� � ID;", CurrPriceCode));
		}

		void CopyToHistory(UInt64 PriceID, string SavedFile)
		{
			string HistoryFileName = DownHistoryPath + PriceID.ToString() + Path.GetExtension(SavedFile);
			try
			{
				File.Copy(SavedFile, HistoryFileName);
			}
			catch { }
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
