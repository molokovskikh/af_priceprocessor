using System;
using System.IO;
using Common.MySql;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Downloader;
using Inforoom.PriceProcessor.Helpers;
using log4net;
using MySql.Data.MySqlClient;
using FileHelper = Common.Tools.FileHelper;

namespace Inforoom.Downloader
{
	/// <summary>
	/// Добавляет медоты для работы с PriceProcessItem
	/// </summary>
	public abstract class BasePriceSourceHandler : BaseSourceHandler
	{
		protected void LogDownloadedPrice(ulong sourceTypeId, string archFileName, string extrFileName)
		{
			With.DeadlockWraper((c, t) => {
				var downloadLogId = DownloadLogEntity.Log(sourceTypeId, (uint)CurrPriceItemId,
					null, null,
					DownPriceResultCode.SuccessDownload, archFileName,
					(String.IsNullOrEmpty(extrFileName)) ? null : Path.GetFileName(extrFileName), c);

				var downloadedFileName = Path.Combine(Settings.Default.InboundPath, "d" + CurrPriceItemId + "_" + downloadLogId + GetExt());
				var item = CreatePriceProcessItem(downloadedFileName);
				item.CopyToInbound(extrFileName, c, t);
				CopyToHistory(downloadLogId);
			});

			using (NDC.Push(CurrPriceItemId.ToString()))
				_logger.InfoFormat("Price {0} - {1} скачан/распакован",
					drCurrent[SourcesTableColumns.colShortName],
					drCurrent[SourcesTableColumns.colPriceName]);
		}

		protected void LogDownloaderFail(ulong sourceTypeId, string message, string archFileName)
		{
			var downloadLogId = DownloadLogEntity.Log(sourceTypeId, CurrPriceItemId, message, DownPriceResultCode.ErrorProcess, archFileName, null);
			if (downloadLogId == 0)
				throw new Exception(String.Format("При логировании прайс-листа {0} получили 0 значение в ID;", CurrPriceItemId));

			CopyToHistory(downloadLogId);
		}

		private string GetExt()
		{
			string FileExt = drCurrent[SourcesTableColumns.colFileExtention].ToString();
			if (String.IsNullOrEmpty(FileExt))
				FileExt = ".err";
			return FileExt;
		}

		protected abstract void CopyToHistory(UInt64 downloadLogId);

		protected virtual PriceProcessItem CreatePriceProcessItem(string normalName)
		{
			var item = new PriceProcessItem(true,
				Convert.ToUInt64(CurrPriceCode),
				CurrCostCode,
				CurrPriceItemId,
				normalName,
				CurrParentSynonym);
			return item;
		}
	}
}