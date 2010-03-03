using System;
using System.Data;
using System.Linq;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Properties;

namespace Inforoom.PriceProcessor.Formalizer.New
{
	public class PriceFormalizationInfo
	{
		public PriceFormalizationInfo(DataRow row)
		{
			PriceName = row[FormRules.colSelfPriceName].ToString();
			FirmShortName = row[FormRules.colFirmShortName].ToString();
			FirmCode = Convert.ToInt64(row[FormRules.colFirmCode]);
			CostCode = (row[FormRules.colCostCode] is DBNull) ? null : (long?)Convert.ToInt64(row[FormRules.colCostCode]);
			PriceCode = Convert.ToUInt32(row[FormRules.colPriceCode]);
			FormByCode = Convert.ToBoolean(row[FormRules.colFormByCode]);
			IsUpdating = Settings.Default.SyncPriceCodes
				.Cast<string>()
				.Select(c => Convert.ToUInt32(c))
				.Any(id => id == PriceCode);
			IsAssortmentPrice = Convert.ToInt32(row[FormRules.colPriceType]) == Settings.Default.ASSORT_FLG;
		}

		public string PriceName { get; set; }
		public string FirmShortName { get; set; }
		public long FirmCode { get; set; }
		//код ценовой колонки, может быть не установлен
		public long? CostCode { get; set; }

		public uint PriceCode { get; set; }

		public bool FormByCode { get; set; }
		public bool IsAssortmentPrice { get; set; }
		public bool IsUpdating { get; set;}
	}
}