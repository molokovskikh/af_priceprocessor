using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.IO;
using Inforoom.Formalizer;
using Inforoom.Logging;
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
                        SimpleLog.Log(this.GetType().Name, "!!!!!!!!!!!!!   drLS.Length < 1");
#endif
                    GetFileFromSource();

                    //SimpleLog.Log(SourceType == "LAN", this.GetType().Name, "Обработали источник с кодом {0} файл '{1}'", dtSources.Rows[0][SourcesTable.colPriceCode], CurrFileName);

                    if (!String.IsNullOrEmpty(CurrFileName))
                    {
                        bool CorrectArchive = true;
                        //Является ли скачанный файл корректным, если нет, то обрабатывать не будем
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
									//todo: это надо включить
                                    //UpdateDB(CurrPriceCode, CurrPriceDate);
                                }
                                else
                                {
									LogDownloaderPrice("Не удалось обработать файл '" + Path.GetFileName(CurrFileName) + "'", DownPriceResultCode.ErrorProcess, Path.GetFileName(CurrFileName), null);
                                }
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

		private void LogDownloaderPrice(string AdditionMessage, DownPriceResultCode resultCode, string ArchFileName, string ExtrFileName)
		{
			ulong PriceID = Logging(CurrPriceItemId, AdditionMessage, resultCode, ArchFileName, (String.IsNullOrEmpty(ExtrFileName)) ? null : Path.GetFileName(ExtrFileName));
			if (PriceID != 0)
			{
				CopyToHistory(PriceID, CurrFileName);
				//Если все сложилось, то копируем файл в Inbound
				if (resultCode == DownPriceResultCode.SuccessDownload)
				{
					string NormalName = FileHelper.NormalizeDir(Settings.Default.InboundPath) + "d" + CurrPriceItemId.ToString() + "_" + PriceID.ToString() + GetExt();
					try
					{
						if (File.Exists(NormalName))
							File.Delete(NormalName);
						File.Copy(ExtrFileName, NormalName);
						PriceProcessItem item = new PriceProcessItem(true, Convert.ToUInt64(CurrPriceCode), CurrCostCode, CurrPriceItemId, NormalName);
						//устанавливаем время загрузки файла
						item.FileTime = CurrPriceDate;
						PriceItemList.AddItem(item);
						SimpleLog.Log(this.GetType().Name + "." + CurrPriceItemId.ToString(), "Price " + (string)drCurrent[SourcesTable.colShortName] + " - " + (string)drCurrent[SourcesTable.colPriceName] + " скачан/распакован");
					}
					catch (Exception ex)
					{
						//todo: по идее здесь не должно возникнуть ошибок, но на всякий случай логируем, возможно надо включить логирование письмом
						//Logging(CurrPriceCode, String.Format("Не удалось перенести файл '{0}' в каталог '{1}'", ExtrFile, NormalName));
						SimpleLog.Log(this.GetType().Name + CurrPriceItemId.ToString(), String.Format("Не удалось перенести файл '{0}' в каталог '{1}': {2} ", ExtrFileName, NormalName, ex));
					}
				}
			}
			else
				throw new Exception(String.Format("При логировании прайс-листа {0} получили 0 значение в ID;", CurrPriceItemId));
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
