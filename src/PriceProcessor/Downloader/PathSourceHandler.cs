using System;
using System.Data;
using System.IO;
using Inforoom.Common;
using Inforoom.PriceProcessor;
using FileHelper=Inforoom.PriceProcessor.Downloader.FileHelper;

namespace Inforoom.Downloader
{
    public abstract class PathSourceHandler : BasePriceSourceHandler
    {
    	protected DateTime GetPriceDateTime()
		{
			var d = (dtSources.Rows[0][SourcesTableColumns.colPriceDate] is DBNull) ? DateTime.MinValue : (DateTime)dtSources.Rows[0][SourcesTableColumns.colPriceDate];
			var item = PriceItemList.GetLastestDownloaded(Convert.ToUInt64(dtSources.Rows[0][SourcesTableColumns.colPriceItemId]));
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
                                    FileHelper.ExtractFromArhive(CurrFileName, CurrFileName + ExtrDirSuffix);
                                }
                                catch (ArchiveHelper.ArchiveException)
                                {
                                    CorrectArchive = false;
                                }
                            }
                            else
                                CorrectArchive = false;
                        }
                        foreach (var drS in drLS)
                        {
                            SetCurrentPriceCode(drS);
                            if (CorrectArchive)
                            {
								var ExtrFile = String.Empty;
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
                        foreach (var drDel in drLS)
                            drDel.Delete();
                    }
                    dtSources.AcceptChanges();
                }
                catch(Exception ex)
                {
                    var Error = String.Empty;
                    if ((drLS != null) && (drLS.Length > 1))
                    {
                        foreach (var drS in drLS)
                        {
                            if (Error == String.Empty)
                                Error += drS[SourcesTableColumns.colPriceCode].ToString();
                            else
                                Error += ", " + drS[SourcesTableColumns.colPriceCode];
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
                        Error = String.Format("�������� : {0}", dtSources.Rows[0][SourcesTableColumns.colPriceCode]);
                        try
                        {
                            dtSources.Rows[0].Delete();
                        }
                        catch { }
                    }
                    Error += Environment.NewLine + Environment.NewLine + ex;
                    LoggingToService(Error);
                    try
                    {
                        dtSources.AcceptChanges();
                    }
                    catch { }
                }
            }
        }

		protected override void CopyToHistory(UInt64 PriceID)
		{
			var HistoryFileName = DownHistoryPath + PriceID + Path.GetExtension(CurrFileName);
			try
			{
				File.Copy(CurrFileName, HistoryFileName);
			}
			catch { }
		}

		protected override PriceProcessItem CreatePriceProcessItem(string normalName)
		{
			var item = base.CreatePriceProcessItem(normalName);
			//������������� ����� �������� �����
			item.FileTime = CurrPriceDate;
			return item;
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
