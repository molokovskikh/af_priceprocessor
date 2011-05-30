using System;
using System.IO;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor;
using FileHelper=Inforoom.Common.FileHelper;

namespace Inforoom.Downloader
{
	public class ClearArchivedPriceSourceHandler : AbstractHandler
	{
		//Время последнего сканирования каталога
		private DateTime lastScan;
		private readonly string _downHistoryPath = FileHelper.NormalizeDir(Settings.Default.HistoryPath);

		public ClearArchivedPriceSourceHandler()
		{
			lastScan = DateTime.MinValue;
        }

		protected override void ProcessData()
		{
			//Сканируем через некоторое время
			if (DateTime.Now.Subtract(lastScan).TotalHours <= Settings.Default.ClearScanInterval) 
				return;

			var archivedPrices = Directory.GetFiles(_downHistoryPath);

			foreach (var priceFile in archivedPrices)
			{
				var fileLastWrite = File.GetLastWriteTime(priceFile);

				//Если разность в днях больше чем в настройки, то файл удяляем
				if (DateTime.Now.Subtract(fileLastWrite).TotalDays > Settings.Default.DepthOfStorageArchivePrices)
					File.Delete(priceFile);
			}

			lastScan = DateTime.Now;
		}

	}
}
