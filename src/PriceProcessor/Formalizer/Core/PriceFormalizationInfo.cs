using System;
using System.Data;
using System.Linq;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Formalizer.Core
{
	public class PriceFormalizationInfo
	{
		public long PriceItemId;
		public long ParentSynonym;
		public long PrevRowCount;
		public Price Price;
		public DataTable FormRulesData;

		public PriceFormalizationInfo()
		{
		}

		public PriceFormalizationInfo(DataRow row, Price price)
		{
			Price = price;
			FormRulesData = row.Table;
			Region = row["region"].ToString();
			CostName = row["CostName"].ToString();
			PriceName = row[FormRules.colSelfPriceName].ToString();
			FirmShortName = row[FormRules.colFirmShortName].ToString();
			FirmCode = Convert.ToInt64(row[FormRules.colFirmCode]);
			CostCode = (row[FormRules.colCostCode] is DBNull) ? null : (long?)Convert.ToInt64(row[FormRules.colCostCode]);
			PriceCode = Convert.ToUInt32(row[FormRules.colPriceCode]);
			FormByCode = Convert.ToBoolean(row[FormRules.colFormByCode]);
			CostType = (CostTypes)Convert.ToInt32(row[FormRules.colCostType]);
			IsUpdating = Settings.Default.SyncPriceCodes
				.Cast<string>()
				.Select(c => Convert.ToUInt32(c))
				.Any(id => id == PriceCode)
				|| price.IsUpdate;
			IsAssortmentPrice = Convert.ToInt32(row[FormRules.colPriceType]) == Settings.Default.ASSORT_FLG;
			PriceItemId = Convert.ToInt64(row[FormRules.colPriceItemId]);
			ParentSynonym = Convert.ToInt64(row[FormRules.colParentSynonym]);
			PrevRowCount = row[FormRules.colPrevRowCount] is DBNull ? 0 : Convert.ToInt64(row[FormRules.colPrevRowCount]);
			CodePage = row[FormRules.colPriceEncode] is DBNull ? 0 : Convert.ToInt32(row[FormRules.colPriceEncode]);
		}

		public string PriceName { get; set; }
		public string FirmShortName { get; set; }
		public string Region { get; set; }
		public long FirmCode { get; set; }
		//код ценовой колонки, может быть не установлен
		public long? CostCode { get; set; }
		public string CostName { get; set; }

		public uint PriceCode { get; set; }

		public bool FormByCode { get; set; }
		public bool IsAssortmentPrice { get; set; }
		public CostTypes CostType { get; set; }
		public bool IsUpdating { get; set; }
		public int CodePage { get; set; }
	}
}