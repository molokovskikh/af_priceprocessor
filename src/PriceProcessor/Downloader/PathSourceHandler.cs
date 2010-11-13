using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Linq;
using Common.Tools;
using Inforoom.Common;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Downloader;
using MySql.Data.MySqlClient;
using FileHelper=Inforoom.PriceProcessor.FileHelper;
using Inforoom.PriceProcessor.Properties;

namespace Inforoom.Downloader
{
	public class PricePreprocessingException : Exception
	{
		public PricePreprocessingException(string message, string fileName) : base(message)
		{
			FileName = fileName;
		}

		public string FileName { get; set; }
	}

	public enum PriceSourceType : ulong
	{
		EMail = 1,
		Http = 2,
		Ftp = 3,
		Lan = 4
	}

	public class PriceSource
	{
		public PriceSource()
		{}

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

		public ulong SourceTypeId
		{
			get
			{
				ulong sourceTypeId = 0;
				var sql = String.Format(@"
select
	src.SourceTypeId
from farm.sources src
	join usersettings.priceItems pim on pim.Id = {0}
where
	src.Id = pim.SourceId", PriceItemId);
				using (var connection = new MySqlConnection(Literals.ConnectionString()))
				{
					connection.Open();
					sourceTypeId = Convert.ToUInt64(MySqlHelper.ExecuteScalar(connection, sql));
				}
				return sourceTypeId;
			}
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

		public virtual DateTime PriceDateTime { get; set; }
		public string ArchivePassword { get; set; }

		public virtual int RequestInterval
		{
			get
			{
				var interval = 0;
				var sql = String.Format(@"
select src.RequestInterval from farm.Sources src
	join usersettings.PriceItems pim on pim.Id = {0}
where src.Id = pim.SourceId", PriceItemId);
				using (var connection = new MySqlConnection(Literals.ConnectionString()))
				{
					connection.Open();
					var res = MySqlHelper.ExecuteScalar(connection, sql);
					if (!Convert.IsDBNull(res))
						interval = Convert.ToInt32(res);
				}
				return interval;
			}
		}
	}

    public abstract class PathSourceHandler : BasePriceSourceHandler
    {
		/// <summary>
		/// Коллекция источников, поледнее обращение к которым завершилось неудачей
		/// </summary>
		protected ArrayList FailedSources = new ArrayList();

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
				if (!CheckDownloadInterval(priceSource))
				{
					currentSource.Delete();
					dtSources.AcceptChanges();
					continue;
				}
				try
				{
					drLS = GetLikeSources(priceSource);
					try
					{
						CurrFileName = String.Empty;
						GetFileFromSource(priceSource);
					}
					catch (PathSourceHandlerException pathException)
					{
						FailedSources.Add(priceSource.PriceItemId);
						DownloadLogEntity.Log(priceSource.SourceTypeId, priceSource.PriceItemId, pathException.ToString(), pathException.ErrorMessage);
					}
					catch (Exception e)
					{
						FailedSources.Add(priceSource.PriceItemId);
						DownloadLogEntity.Log(priceSource.SourceTypeId, priceSource.PriceItemId, e.ToString());
					}

					if (!String.IsNullOrEmpty(CurrFileName))
					{
						var correctArchive = ProcessArchiveIfNeeded(priceSource);
						foreach (var drS in drLS)
						{
							SetCurrentPriceCode(drS);
							string extractFile = null;
							try
							{
								if (!correctArchive)
									throw new PricePreprocessingException("Не удалось распаковать файл '" + Path.GetFileName(CurrFileName) + "'. Файл поврежден", CurrFileName);

								if (!ProcessPriceFile(CurrFileName, out extractFile, priceSource.SourceTypeId))
									throw new PricePreprocessingException("Не удалось обработать файл '" + Path.GetFileName(CurrFileName) + "'", CurrFileName);

								LogDownloadedPrice(priceSource.SourceTypeId, Path.GetFileName(CurrFileName), extractFile);
								FileProcessed();
							}
							catch (PricePreprocessingException e)
							{
								LogDownloaderFail(priceSource.SourceTypeId, e.Message, e.FileName);
								FileProcessed();
							}
							catch (Exception e)
							{
								LogDownloaderFail(priceSource.SourceTypeId, e.Message, extractFile);
							}
							finally
							{
								drS.Delete();
							}
						}
						Cleanup();
					}
					else
					{
						foreach (var drDel in drLS)
							drDel.Delete();
					}
				}
				catch (Exception ex)
				{
					var error = String.Empty;
					if (drLS != null && drLS.Length > 1)
					{
						error += String.Join(", ", drLS.Select(r => r[SourcesTableColumns.colPriceCode].ToString()).ToArray());
						drLS.Each(r => FileHelper.Safe(r.Delete));
						error = "Источники : " + error;
					}
					else
					{
						error = String.Format("Источник : {0}", currentSource[SourcesTableColumns.colPriceCode]);
						FileHelper.Safe(currentSource.Delete);
					}
					error += Environment.NewLine + Environment.NewLine + ex;
					LoggingToService(error);
				}
				finally
				{
					FileHelper.Safe(() => dtSources.AcceptChanges());
				}
			}
		}

		/// <summary>
		/// Проверяет, истек ли интервал, спустя который нужно обращаться к источнику
		/// </summary>
		/// <returns>
		/// true - интервал истек, нужно обратиться
		/// false - интервал еще не истек, не нужно обращаться
		/// </returns>
		protected bool CheckDownloadInterval(PriceSource source)
		{
			// downloadInterval - в секундах
			var downloadInterval = source.RequestInterval;
			if (FailedSources.Contains(source.PriceItemId))
			{
				FailedSources.Remove(source.PriceItemId);
				downloadInterval = 0;
			}
			var seconds = DateTime.Now.Subtract(source.PriceDateTime).Hours*3600 +
			              DateTime.Now.Subtract(source.PriceDateTime).Minutes*60 +
			              DateTime.Now.Subtract(source.PriceDateTime).Seconds;
			return (seconds >= downloadInterval);
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

		protected override void CopyToHistory(UInt64 downloadLogId)
		{
			var HistoryFileName = DownHistoryPath + downloadLogId + Path.GetExtension(CurrFileName);
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
		protected abstract DataRow[] GetLikeSources(PriceSource currentSource);

		protected virtual void FileProcessed() { }
	}

	public class PathSourceHandlerException : Exception
	{
		public static string NetworkErrorMessage = "Ошибка сетевого соединения";

		public static string ThreadAbortErrorMessage = "Загрузка файла была прервана";

		public PathSourceHandlerException()
		{ }

		public PathSourceHandlerException(string message, Exception innerException)
			: base(message, innerException)
		{
			ErrorMessage = message;
		}

		public string ErrorMessage { get; set; }

		protected virtual string GetShortErrorMessage(Exception e)
		{
			return NetworkErrorMessage;
		}
	}
}
