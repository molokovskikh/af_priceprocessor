using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class GenesisMMskSpecialParser : BaseDbfParser
	{
		 public override DbfParser GetParser()
        {
            return new DbfParser()
                .DocumentHeader(d => d.ProviderDocumentId, "TRX_NUMBER")
                .DocumentHeader(d => d.DocumentDate, "TRX_DATE")
				.Line(l => l.Code, "ITEM_ID")
                .Line(l => l.Product, "ITEM_NAME")
				.Line(l => l.Quantity, "QNTY")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE")
				.Line(l => l.Nds, "TAX_RATE")
				.Line(l => l.NdsAmount, "TAX_AMOUNT")
				.Line(l => l.Amount, "FULL_AMNT")
				.Line(l => l.SerialNumber, "LOT_NUMBER")
				.Line(l => l.Period, "EXP_DATE")
				.Line(l => l.Certificates, "CER_NUMBER")
				.Line(l => l.Producer, "VE_NAME")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE_VR")
				.Line(l => l.Country, "VE_COUNTRY")
				.Line(l => l.BillOfEntryNumber, "DECL_NUM")
				.Line(l => l.EAN13, "EAN13");
        }

        public static bool CheckFileFormat(DataTable data)
        {
        	return data.Columns.Contains("TRX_NUMBER")
        	       && data.Columns.Contains("TRX_DATE")
        	       && data.Columns.Contains("ITEM_ID")
        	       && data.Columns.Contains("ITEM_NAME")
        	       && data.Columns.Contains("QNTY")
        	       && data.Columns.Contains("TAX_RATE");
        }
	}
}
