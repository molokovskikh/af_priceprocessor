using System;
using System.Data;
using System.IO;
using Inforoom.Common;
using Inforoom.PriceProcessor;
using FileHelper=Inforoom.PriceProcessor.FileHelper;

namespace Inforoom.Downloader
{
	public class PriceSource
	{
		public PriceSource(DataRow currentSource)
		{
			PriceItemId = Convert.ToUInt32(currentSource[SourcesTableColumns.colPriceItemId]);
			PricePath = currentSource[SourcesTableColumns.colPricePath].ToString().Trim();
			PriceMask = currentSource[SourcesTableColumns.colPriceMask].ToString();

			HttpLogin = currentSource[SourcesTableColumns.colHTTPLogin].ToString();
			HttpPassword = currentSource[SourcesTableColumns.colHTTPPassword].ToString();

			FtpDir = currentSource[SourcesTableColumns.colFTPDir].ToString();
			FtpLogin = currentSource[SourcesTableColumns.colFTPLogin].ToString();
			FtpPassword = currentSource[SourcesTableColumns.colFTPPassword].ToString();
			FtpPassiveMode = Convert.ToByte(currentSource[SourcesTableColumns.colFTPPassiveMode]) == 1;

			FirmCode = currentSource[SourcesTableColumns.colFirmCode];

			ArchivePassword = currentSource["ArchivePassword"].ToString();

			if (currentSource["LastDownload"] is DBNull)
				PriceDateTime = DateTime.MinValue;
			else 
				PriceDateTime = Convert.ToDateTime(currentSource["LastDownload"]);
		}

		public uint PriceItemId { get; set; }
		public string PricePath { get; set; }
		public string PriceMask { get; set; }

		public string HttpLogin { get; set; }
		public string HttpPassword { get; set; }

		public string FtpDir { get; set; }
		public string FtpLogin { get; set; }
		public string FtpPassword { get; set; }
		public bool FtpPassiveMode { get; set; }

		public object FirmCode { get; set; }

		public DateTime PriceDateTime { get; set; }
		public string ArchivePassword { get; set; }
	}

    public abstract class PathSourceHandler : BasePriceSourceHandler
    {
        protected override void ProcessData()
        {
            //набор строк похожих источников
            DataRow[] drLS;
            FillSourcesTable();
            while (dtSources.Rows.Count > 0)
            {
                drLS = null;
            	var currentSource = dtSources.Rows[0];
            	var priceSource = new PriceSource(currentSource);
            	try
                {
                    drLS = GetLikeSources(priceSource);
#if DEBUG
					if (drLS.Length < 1)
						_logger.Debug("!!!!!!!!!!!!!   drLS.Length < 1");
#endif
                    GetFileFromSource(priceSource);

                    if (!String.IsNullOrEmpty(CurrFileName))
                    {
                        var correctArchive = ProcessArchiveIfNeeded(priceSource);
                    	foreach (var drS in drLS)
                        {
                            SetCurrentPriceCode(drS);
                            if (correctArchive)
                            {
								var ExtrFile = String.Empty;
								if (ProcessPriceFile(CurrFileName, out ExtrFile))
									LogDownloaderPrice(null, DownPriceResultCode.SuccessDownload, Path.GetFileName(CurrFileName), ExtrFile);
                                else
									LogDownloaderPrice("Не удалось обработать файл '" + Path.GetFileName(CurrFileName) + "'", DownPriceResultCode.ErrorProcess, Path.GetFileName(CurrFileName), null);
                            }
                            else
                            {
								LogDownloaderPrice("Не удалось распаковать файл '" + Path.GetFileName(CurrFileName) + "'", DownPriceResultCode.ErrorProcess, Path.GetFileName(CurrFileName), null);
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
							FileHelper.Safe(() => drS.Delete());
                        }
                        Error = "Источники : " + Error;
                    }
                    else
                    {
                        Error = String.Format("Источник : {0}", currentSource[SourcesTableColumns.colPriceCode]);
						FileHelper.Safe(currentSource.Delete);
                    }
                    Error += Environment.NewLine + Environment.NewLine + ex;
                    LoggingToService(Error);
					FileHelper.Safe(() => dtSources.AcceptChanges());
                }
            }
        }

    	private bool ProcessArchiveIfNeeded(PriceSource priceSource)
    	{
    		bool CorrectArchive = true;
    		//Является ли скачанный файл корректным, если нет, то обрабатывать не будем
    		if (ArchiveHelper.IsArchive(CurrFileName))
    		{
    			if (ArchiveHelper.TestArchive(CurrFileName, priceSource.ArchivePassword))
    			{
    				try
    				{
    					FileHelper.ExtractFromArhive(CurrFileName, CurrFileName + ExtrDirSuffix, priceSource.ArchivePassword);
    				}
    				catch (ArchiveHelper.ArchiveException)
    				{
    					CorrectArchive = false;
    				}
    			}
    			else
    				CorrectArchive = false;
    		}
    		return CorrectArchive;
    	}

    	protected override void CopyToHistory(UInt64 PriceID)
		{
			var HistoryFileName = DownHistoryPath + PriceID + Path.GetExtension(CurrFileName);
			FileHelper.Safe(() => File.Copy(CurrFileName, HistoryFileName));
		}

		protected override PriceProcessItem CreatePriceProcessItem(string normalName)
		{
			var item = base.CreatePriceProcessItem(normalName);
			//устанавливаем время загрузки файла
			item.FileTime = CurrPriceDate;
			return item;
		}

    	/// <summary>
        /// Получает файл из источника, взятого из таблицы первым
        /// </summary>
        protected abstract void GetFileFromSource(PriceSource row);

        /// <summary>
        /// Получить прайс-листы, у которых истоники совпадают с первым в списке
        /// </summary>
        /// <returns></returns>
        protected abstract DataRow[] GetLikeSources(PriceSource currentSource);
    }
}
