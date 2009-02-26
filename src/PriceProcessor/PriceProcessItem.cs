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
		public DateTime CreateTime { get; private set; }

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
		}

		public static PriceProcessItem TryToLoadPriceProcessItem(string filename)
		{
			var drPriceItem = MySqlHelper.ExecuteDataRow(
				Literals.ConnectionString(),
@"select
  pc.PriceCode as PriceCode,
  if(pd.CostType = 1, pc.CostCode, null) CostCode,
  pc.PriceItemId,
  pd.ParentSynonym
from
  usersettings.pricescosts pc,
  usersettings.pricesdata pd
where
    pc.PriceItemId = ?FileName
and ((pd.CostType = 1) or (pc.BaseCost = 1))
and pd.PriceCode = pc.PriceCode",
				new MySqlParameter("?FileName", Path.GetFileNameWithoutExtension(filename)));
			if (drPriceItem != null)
			{
				var priceCode = Convert.ToUInt64(drPriceItem["PriceCode"]);
				var costCode = (drPriceItem["CostCode"] is DBNull) ? null : (ulong?)Convert.ToUInt64(drPriceItem["CostCode"]);
				var priceItemId = Convert.ToUInt64(drPriceItem["PriceItemId"]);
				var parentSynonym = (drPriceItem["ParentSynonym"] is DBNull) ? null : (ulong?)Convert.ToUInt64(drPriceItem["ParentSynonym"]);
				return new PriceProcessItem(false, priceCode, costCode, priceItemId, filename, parentSynonym);
			}
			return null;
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
			return !processList.Select(t => t.ProcessItem).Where(i => i != this).All(IsSynonymEqual);
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

			return false;
		}
	}
}
