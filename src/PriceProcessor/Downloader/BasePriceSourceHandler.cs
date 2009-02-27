using System;
using System.IO;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Properties;
using FileHelper=Inforoom.Common.FileHelper;

namespace Inforoom.Downloader
{
	/// <summary>
	/// ƒобавл€ет педоты дл€ работы с PriceProcessItem
	/// </summary>
	public abstract class BasePriceSourceHandler : BaseSourceHandler
	{
		protected void LogDownloaderPrice(string AdditionMessage, DownPriceResultCode resultCode, string ArchFileName, string extrFileName)
		{
			var downloadLogId = Logging(CurrPriceItemId, AdditionMessage, resultCode, ArchFileName, (String.IsNullOrEmpty(extrFileName)) ? null : Path.GetFileName(extrFileName));
			if (downloadLogId == 0)
				throw new Exception(String.Format("ѕри логировании прайс-листа {0} получили 0 значение в ID;", CurrPriceItemId));

			CopyToHistory(downloadLogId);

			if (resultCode != DownPriceResultCode.SuccessDownload)
				return;

			//≈сли все сложилось, то копируем файл в Inbound
			var normalName = FileHelper.NormalizeDir(Settings.Default.InboundPath) + "d" + CurrPriceItemId + "_" + downloadLogId + GetExt();
			try
			{
				CreatePriceProcessItem(normalName).CopyToInbound(extrFileName);
				using (log4net.NDC.Push(CurrPriceItemId.ToString()))
					_logger.InfoFormat("Price {0} - {1} скачан/распакован",
					                   drCurrent[SourcesTableColumns.colShortName],
					                   drCurrent[SourcesTableColumns.colPriceName]);
			}
			catch (Exception ex)
			{
				//todo: по идее здесь не должно возникнуть ошибок, но на вс€кий случай логируем, возможно надо включить логирование письмом
				using (log4net.NDC.Push(CurrPriceItemId.ToString()))
					_logger.ErrorFormat("Ќе удалось перенести файл '{0}' в каталог '{1}'\r\n{2}",
					                    extrFileName,
					                    normalName,
					                    ex);
			}
		}

		protected abstract void CopyToHistory(UInt64 PriceID);

		protected virtual PriceProcessItem CreatePriceProcessItem(string normalName)
		{
			var item = new PriceProcessItem(true,
			                                Convert.ToUInt64(CurrPriceCode),
			                                CurrCostCode,
			                                CurrPriceItemId,
			                                normalName,
			                                CurrParentSynonym)
			           	{
							IsMyPrice = IsMyPrice,
			           	};
			return item;
		}
	}
}