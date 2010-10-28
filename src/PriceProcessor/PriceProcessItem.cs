using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Inforoom.Formalizer;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor
{
	public class PriceProcessItem
	{
		//Скачан ли прайс-лист или переподложен
		public bool Downloaded { get; private set; }

		//путь к прайс-листу: либо папа Inbound на fms или временная папка для скаченных прайсов
		public string FilePath { get; set; }

		//код прайс-листа
		public ulong PriceCode { get; private set; }

		//код цены, может быть null
		public ulong? CostCode { get; private set; }

		//Id из таблицы PriceItems
		public ulong PriceItemId { get; private set; }

		//Дата файла, взятая из сети, ftp или http, чтобы определить: необходимо ли скачивать еще раз данный прайс?
		public DateTime? FileTime { get; set; }

		//Дата создания элемента, чтобы знать: можно ли его брать в обработку или нет
		public DateTime CreateTime { get; /* для теста private */set; }

		private ulong? ParentSynonym { get; set; }

		public PriceProcessItem(bool downloaded, ulong priceCode, ulong? costCode, ulong priceItemId, string filePath, ulong? parentSynonym)
		{
			Downloaded = downloaded;
			PriceCode = priceCode;
			CostCode = costCode;
			PriceItemId = priceItemId;
			FilePath = filePath;
			CreateTime = DateTime.UtcNow;
			ParentSynonym = parentSynonym;
			if (downloaded)
				FileTime = DateTime.Now;
		}

		public static PriceProcessItem TryToLoadPriceProcessItem(string filename)
		{
			uint priceItemId;
			var isDownloaded = IsDownloadedPrice(filename);
			if (isDownloaded)
				priceItemId = Convert.ToUInt32(Path.GetFileName(filename).Substring(1, Path.GetFileName(filename).IndexOf("_") - 1));
			else
				priceItemId = Convert.ToUInt32(Path.GetFileNameWithoutExtension(filename));

			var drPriceItem = MySqlHelper.ExecuteDataRow(
				Literals.ConnectionString(),
@"select
  pc.PriceCode as PriceCode,
  if(pd.CostType = 1, pc.CostCode, null) CostCode,
  pc.PriceItemId,
  pd.ParentSynonym,
  pi.LastDownload
from (usersettings.pricescosts pc, usersettings.pricesdata pd)
	join usersettings.priceitems pi on pc.PriceItemId = pi.Id
where pc.PriceItemId = ?PriceItemId
	  and ((pd.CostType = 1) or (pc.BaseCost = 1))
	  and pd.PriceCode = pc.PriceCode
group by pi.Id",
				new MySqlParameter("?PriceItemId", priceItemId));
			if (drPriceItem == null)
				return null;

			var priceCode = Convert.ToUInt64(drPriceItem["PriceCode"]);
			var costCode = (drPriceItem["CostCode"] is DBNull) ? null : (ulong?)Convert.ToUInt64(drPriceItem["CostCode"]);
			var parentSynonym = (drPriceItem["ParentSynonym"] is DBNull) ? null : (ulong?)Convert.ToUInt64(drPriceItem["ParentSynonym"]);
			var lastDownload = (drPriceItem["LastDownload"] is DBNull) ? DateTime.MinValue : Convert.ToDateTime(drPriceItem["LastDownload"]);
			var item = new PriceProcessItem(isDownloaded, priceCode, costCode, priceItemId, filename, parentSynonym);

			if (isDownloaded)
				item.FileTime = lastDownload;

			return item;
		}

		public bool IsReadyForProcessing(IEnumerable<PriceProcessThread> processList)
		{
			//Если разница между временем создания элемента в PriceItemList и текущим временем больше 5 секунд, то берем файл в обработку
			var isSeasoned = DateTime.UtcNow.Subtract(CreateTime).TotalSeconds > 5;
			if (!isSeasoned)
				return false;

			if (processList.Count() == 0)
				return true;

			//не запущен ли он уже в работу?
			var isProcessing = processList.Any(thread => thread.ProcessItem == this);
			if (isProcessing)
				return false;

			//Не формализуется ли прайс-лист с такими же синонимами?
			return !processList.Select(t => t.ProcessItem).Where(i => i != this).Any(IsSynonymEqual);
		}

		public bool IsSynonymEqual(PriceProcessItem item)
		{
			if (ParentSynonym != null
				&& item.ParentSynonym != null 
				&& ParentSynonym == item.ParentSynonym)
				return true;

			if (ParentSynonym != null
				&& ParentSynonym == item.PriceCode)
				return true;

			if (item.ParentSynonym != null 
				&& item.ParentSynonym == PriceCode)
				return true;

			if (item.ParentSynonym == null &&
				ParentSynonym == null &&
				item.PriceCode == PriceCode)
				return true;

			return false;
		}

		public void CopyToInbound(string extrFileName, MySqlConnection connection, MySqlTransaction transaction)
		{
			var command = new MySqlCommand(@"
update usersettings.PriceItems 
set LastDownload = ?FileTime
where Id = ?Id", connection, transaction);
			command.Parameters.AddWithValue("?Id", PriceItemId);
			command.Parameters.AddWithValue("?FileTime", FileTime);
			command.ExecuteNonQuery();

			if (File.Exists(FilePath))
				File.Delete(FilePath);
			File.Copy(extrFileName, FilePath);
			PriceItemList.AddItem(this);
		}

		public static bool IsDownloadedPrice(string priceFile)
		{
			return Path.GetFileName(priceFile).StartsWith("d", StringComparison.OrdinalIgnoreCase);
		}
	}
}
