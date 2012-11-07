using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class OriolaStavropol13525Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "TRX_NUM")
				.DocumentHeader(h => h.DocumentDate, "TRX_DATE")
				.DocumentInvoice(i => i.BuyerName, "CUSTOMER")
				.DocumentInvoice(i => i.BuyerAddress, "SHIP_TO")
				.Line(l => l.Code, "ITEM_ID")
				.Line(l => l.Product, "ITEM_NAME")
				.Line(l => l.Producer, "VEND_NAME")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE_VR")
				.Line(l => l.BillOfEntryNumber, "DECL_NUM")
				.Line(l => l.Nds, "TAX_RATE")
				.Line(l => l.NdsAmount, "TAX_AMOUNT")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE")
				.Line(l => l.SupplierCost, "PRICE_TAX")
				.Line(l => l.Amount, "FULL_AMNT")
				.Line(l => l.Quantity, "QNTY")
				.Line(l => l.SerialNumber, "LOT_NUMBER")
				.Line(l => l.Certificates, "CER_NUMBER")
				.Line(l => l.Period, "EXP_DATE")
				.Line(l => l.VitallyImportant, "IS_LIFE")
				.Line(l => l.RegistryCost, "CENA_REEST")
				.Line(l => l.Country, "STRANA")
				.Line(l => l.EAN13, "SHTRIH_KOD");
		}

		public static bool CheckFileFormat(DataTable table)
		{
			return table.Columns.Contains("TRX_DATE") &&
				table.Columns.Contains("PRICE_VR") &&
				table.Columns.Contains("EXP_DATE") &&
				table.Columns.Contains("STRANA") &&
				table.Columns.Contains("PRICE_TAX");
		}
	}
}
