using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.IO;
using Inforoom.Formalizer;
using Inforoom.Common;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Properties;

namespace Inforoom.Downloader
{
    public abstract class PathSourceHandler : BaseSourceHandler
    {
        public PathSourceHandler()
            : base()
        { 
        }

		protected DateTime GetPriceDateTime()
		{
			DateTime d = (dtSources.Rows[0][SourcesTable.colPriceDate] is DBNull) ? DateTime.MinValue : (DateTime)dtSources.Rows[0][SourcesTable.colPriceDate];
			PriceProcessItem item = PriceItemList.GetLastestDownloaded(Convert.ToUInt64(dtSources.Rows[0][SourcesTable.colPriceItemId]));
			if (item != null)
				d = item.FileTime.Value;
			return d;
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
						_logger.Debug("!!!!!!!!!!!!!   drLS.Length < 1");
#endif
                    GetFileFromSource();

                    if (!String.IsNullOrEmpty(CurrFileName))
                    {
                        bool CorrectArchive = true;
                        //�������� �� ��������� ���� ����������, ���� ���, �� ������������ �� �����
                        if (ArchiveHelper.IsArchive(CurrFileName))
                        {
                            if (ArchiveHelper.TestArchive(CurrFileName))
                            {
                                try
                                {
                                    ExtractFromArhive(CurrFileName, CurrFileName + ExtrDirSuffix);
                                }
                                catch (ArchiveHelper.ArchiveException)
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
									LogDownloaderPrice(null, DownPriceResultCode.SuccessDownload, Path.GetFileName(CurrFileName), ExtrFile);
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
			ulong PriceID = Logging(CurrPriceItemId, AdditionMessage, resultCode, ArchFileName, (String.IsNullOrEmpty(ExtrFileName)) ? null : Path.GetFileName(ExtrFileName));
			if (PriceID != 0)
			{
				CopyToHistory(PriceID, CurrFileName);
				//���� ��� ���������, �� �������� ���� � Inbound
				if (resultCode == DownPriceResultCode.SuccessDownload)
				{
					string NormalName = FileHelper.NormalizeDir(Settings.Default.InboundPath) + "d" + CurrPriceItemId.ToString() + "_" + PriceID.ToString() + GetExt();
					try
					{
						if (File.Exists(NormalName))
							File.Delete(NormalName);
						File.Copy(ExtrFileName, NormalName);
						PriceProcessItem item = new PriceProcessItem(true, Convert.ToUInt64(CurrPriceCode), CurrCostCode, CurrPriceItemId, NormalName);
						//������������� ����� �������� �����
						item.FileTime = CurrPriceDate;
						PriceItemList.AddItem(item);
						using (log4net.NDC.Push(CurrPriceItemId.ToString()))
							_logger.InfoFormat("Price {0} - {1} ������/����������", drCurrent[SourcesTable.colShortName], drCurrent[SourcesTable.colPriceName]);
					}
					catch (Exception ex)
					{
						//todo: �� ���� ����� �� ������ ���������� ������, �� �� ������ ������ ��������, �������� ���� �������� ����������� �������
						using (log4net.NDC.Push(CurrPriceItemId.ToString()))
							_logger.ErrorFormat("�� ������� ��������� ���� '{0}' � ������� '{1}'\r\n{2}", ExtrFileName, NormalName, ex);
					}
				}
			}
			else
				throw new Exception(String.Format("��� ����������� �����-����� {0} �������� 0 �������� � ID;", CurrPriceItemId));
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
