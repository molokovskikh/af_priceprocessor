using System;
using System.Collections.Generic;
using System.Text;

namespace Inforoom.PriceProcessor
{
	public static class PriceItemList
	{
		public static List<PriceProcessItem> list = new List<PriceProcessItem>();
 
		//Возвращает true, если был добавлен в очередь, если false, значит есть скаченный, то добавлять не надо
		public static bool AddItem(PriceProcessItem item)
		{
			lock (list)
			{
				//Если элемент только что скачан, то добавляем его в список
				if (item.Downloaded)
				{
					list.Add(item);
					return true;
				}
				else
				{
					//Если файл перепровели, то проверяем на существование скаченного
					if (DownloadedExists(item.PriceItemId))
						return false;
					else
					{
						list.Add(item);
						return true;
					}
				}
			}
		}

		//получить последний скаченный прайс
		public static PriceProcessItem GetLastestDownloaded(ulong PriceItemId)
		{
			List<PriceProcessItem> downloadedList = list.FindAll(delegate(PriceProcessItem item) { return item.Downloaded && (item.PriceItemId == PriceItemId); });
			if (downloadedList.Count > 0)
			{
				downloadedList.Sort(delegate(PriceProcessItem a, PriceProcessItem b) { if (a.FileTime > b.FileTime) return -1; else return 1; });
				return downloadedList[0];
			}
			else
				return null;
		}

		public static bool DownloadedExists(ulong PriceItemId)
		{

			return list.Exists(delegate(PriceProcessItem item) { return (item.Downloaded && (item.PriceItemId == PriceItemId)); });
		}

		public static List<PriceProcessItem> FindAllByPriceItemId(ulong PriceItemId)
		{
			return list.FindAll(delegate(PriceProcessItem item) { return (item.PriceItemId == PriceItemId); });
		}

		public static List<PriceProcessItem> GetDownloadedItemList()
		{
			return list.FindAll(delegate(PriceProcessItem item) { return (item.Downloaded); });
		}

		public static int GetDownloadedCount()
		{ 
			return GetDownloadedItemList().Count;
		}

		public static DateTime? GetFileTime(ulong PriceItemId)
		{
			PriceProcessItem findItem = list.Find(delegate(PriceProcessItem item) { return (item.Downloaded && (item.PriceItemId == PriceItemId)); });
			if (findItem != null)
				return findItem.FileTime;
			else
				return null;
		}
	}
}
