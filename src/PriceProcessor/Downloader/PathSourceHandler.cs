using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Linq;
using Common.Tools;
using Inforoom.Common;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Downloader;
using FileHelper = Inforoom.PriceProcessor.FileHelper;

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

	public abstract class PathSourceHandler : BasePriceSourceHandler
	{
		/// <summary>
		/// Коллекция источников, поледнее обращение к которым завершилось неудачей
		/// </summary>
		public ArrayList FailedSources = new ArrayList();

		public override void ProcessData()
		{
			//набор строк похожих источников
			FillSourcesTable();
			while (dtSources.Rows.Count > 0) {
				DataRow[] drLS = null;
				var currentSource = dtSources.Rows[0];
				var priceSource = new PriceSource(currentSource);
				if (!IsReadyForDownload(priceSource)) {
					currentSource.Delete();
					dtSources.AcceptChanges();
					continue;
				}
				try {
					drLS = GetLikeSources(priceSource);
					try {
						CurrFileName = String.Empty;
						GetFileFromSource(priceSource);
						priceSource.UpdateLastCheck();
					}
					catch (PathSourceHandlerException pathException) {
						FailedSources.Add(priceSource.PriceItemId);
						DownloadLogEntity.Log(priceSource.SourceTypeId, priceSource.PriceItemId, pathException.ToString(), pathException.ErrorMessage);
					}
					catch (Exception e) {
						FailedSources.Add(priceSource.PriceItemId);
						DownloadLogEntity.Log(priceSource.SourceTypeId, priceSource.PriceItemId, e.ToString());
					}

					if (!String.IsNullOrEmpty(CurrFileName)) {
						var correctArchive = FileHelper.ProcessArchiveIfNeeded(CurrFileName, ExtrDirSuffix, priceSource.ArchivePassword);
						foreach (var drS in drLS) {
							SetCurrentPriceCode(drS);
							string extractFile = null;
							try {
								if (!correctArchive)
									throw new PricePreprocessingException("Не удалось распаковать файл '" + Path.GetFileName(CurrFileName) + "'. Файл поврежден", CurrFileName);

								if (!ProcessPriceFile(CurrFileName, out extractFile, priceSource.SourceTypeId))
									throw new PricePreprocessingException("Не удалось обработать файл '" + Path.GetFileName(CurrFileName) + "'", CurrFileName);

								LogDownloadedPrice(priceSource.SourceTypeId, Path.GetFileName(CurrFileName), extractFile);
								FileProcessed();
							}
							catch (PricePreprocessingException e) {
								LogDownloaderFail(priceSource.SourceTypeId, e.Message, e.FileName);
								FileProcessed();
							}
							catch (Exception e) {
								LogDownloaderFail(priceSource.SourceTypeId, e.Message, extractFile);
							}
							finally {
								drS.Delete();
							}
						}
						Cleanup();
					}
					else {
						foreach (var drDel in drLS)
							drDel.Delete();
					}
				}
				catch (Exception ex) {
					var error = String.Empty;
					if (drLS != null && drLS.Length > 1) {
						error += String.Join(", ", drLS.Select(r => r[SourcesTableColumns.colPriceCode].ToString()).ToArray());
						drLS.Each(r => FileHelper.Safe(r.Delete));
						error = "Источники : " + error;
					}
					else {
						error = String.Format("Источник : {0}", currentSource[SourcesTableColumns.colPriceCode]);
						FileHelper.Safe(currentSource.Delete);
					}
					Log(ex, error);
				}
				finally {
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
		public bool IsReadyForDownload(PriceSource source)
		{
			// downloadInterval - в секундах
			if (FailedSources.Contains(source.PriceItemId)) {
				FailedSources.Remove(source.PriceItemId);
				return true;
			}
			return source.IsReadyForDownload();
		}

		protected override void CopyToHistory(UInt64 downloadLogId)
		{
			var historyFileName = Path.Combine(DownHistoryPath, downloadLogId + Path.GetExtension(CurrFileName));
			FileHelper.Safe(() => File.Copy(CurrFileName, historyFileName));
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
		public abstract DataRow[] GetLikeSources(PriceSource currentSource);

		protected virtual void FileProcessed()
		{
		}
	}

	public class PathSourceHandlerException : Exception
	{
		public static string NetworkErrorMessage = "Ошибка сетевого соединения";

		public static string ThreadAbortErrorMessage = "Загрузка файла была прервана";

		public PathSourceHandlerException()
		{
		}

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