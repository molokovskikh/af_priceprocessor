using System;
using System.IO;
using Inforoom.PriceProcessor;

namespace Inforoom.Downloader
{
	public class ClearArchivedPriceSourceHandler : AbstractHandler
	{
		//Время последнего сканирования каталога
		private DateTime lastScan;
		private readonly string _downHistoryPath = Settings.Default.HistoryPath;

		public ClearArchivedPriceSourceHandler()
		{
			lastScan = DateTime.MinValue;
		}

		public override void ProcessData()
		{
			//Сканируем через некоторое время
			if (DateTime.Now.Subtract(lastScan).TotalHours <= Settings.Default.ClearScanInterval)
				return;

			var archivedPrices = Directory.GetFiles(_downHistoryPath);

			foreach (var priceFile in archivedPrices) {
				var fileLastWrite = File.GetLastWriteTime(priceFile);

				//Если разность в днях больше чем в настройки, то файл удяляем
				if (DateTime.Now.Subtract(fileLastWrite).TotalDays > Settings.Default.DepthOfStorageArchivePrices)
					File.Delete(priceFile);
			}

			lastScan = DateTime.Now;
		}
	}
}