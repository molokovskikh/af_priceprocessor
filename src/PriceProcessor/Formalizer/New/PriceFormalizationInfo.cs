using System;
using System.Data;
using System.Linq;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor;

namespace Inforoom.PriceProcessor.Formalizer.New
{
	public class PriceFormalizationInfo
	{
		public long PriceItemId;
		public long ParentSynonym;
		public long PrevRowCount;

		public PriceFormalizationInfo(DataRow row)
		{
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
				.Any(id => id == PriceCode);
			IsAssortmentPrice = Convert.ToInt32(row[FormRules.colPriceType]) == Settings.Default.ASSORT_FLG;
			PriceItemId = Convert.ToInt64(row[FormRules.colPriceItemId]);
			ParentSynonym = Convert.ToInt64(row[FormRules.colParentSynonym]);

			PricePurpose = PricePurpose.Normal;
			var firmSegment = Convert.ToInt16(row[FormRules.colFirmSegment]);
			if (IsAssortmentPrice)
				PricePurpose |= PricePurpose.Assortment;
			if (firmSegment == 1)
				PricePurpose |= PricePurpose.Helper;
			PrevRowCount = row[FormRules.colPrevRowCount] is DBNull ? 0 : Convert.ToInt64(row[FormRules.colPrevRowCount]);
		}

		public string PriceName { get; set; }
		public string FirmShortName { get; set; }
		public long FirmCode { get; set; }
		//код ценовой колонки, может быть не установлен
		public long? CostCode { get; set; }

		public uint PriceCode { get; set; }

		public bool FormByCode { get; set; }
		public bool IsAssortmentPrice { get; set; }
		public CostTypes CostType { get; set; }
		public bool IsUpdating { get; set; }

		public PricePurpose PricePurpose { get; set; }
	}
}