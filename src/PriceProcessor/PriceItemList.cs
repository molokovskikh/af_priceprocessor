using System;
using System.Collections.Generic;

namespace Inforoom.PriceProcessor
{
	public static class PriceItemList
	{
		public static List<PriceProcessItem> list = new List<PriceProcessItem>();

		//Возвращает true, если был добавлен в очередь, если false, значит есть скаченный, то добавлять не надо
		public static bool AddItem(PriceProcessItem item)
		{
			lock (list) {
				//Если элемент только что скачан, то добавляем его в список
				if (item.Downloaded) {
					list.Add(item);
					return true;
				}
				//Если файл перепровели, то проверяем на существование скаченного
				if (DownloadedExists(item.PriceItemId))
					return false;
				list.Add(item);
				return true;
			}
		}

		//получить последний скаченный прайс
		public static PriceProcessItem GetLastestDownloaded(ulong PriceItemId)
		{
			var downloadedList = list.FindAll(item => item.Downloaded && (item.PriceItemId == PriceItemId));
			if (downloadedList.Count > 0) {
				downloadedList.Sort(delegate(PriceProcessItem a, PriceProcessItem b) {
					if (a.FileTime > b.FileTime) return -1;
					else return 1;
				});
				return downloadedList[0];
			}
			return null;
		}

		public static bool DownloadedExists(ulong PriceItemId)
		{
			return list.Exists(item => (item.Downloaded && (item.PriceItemId == PriceItemId)));
		}

		public static List<PriceProcessItem> FindAllByPriceItemId(ulong PriceItemId)
		{
			return list.FindAll(item => (item.PriceItemId == PriceItemId));
		}

		public static List<PriceProcessItem> GetDownloadedItemList()
		{
			return list.FindAll(item => (item.Downloaded));
		}

		public static int GetDownloadedCount()
		{
			return GetDownloadedItemList().Count;
		}
	}
}