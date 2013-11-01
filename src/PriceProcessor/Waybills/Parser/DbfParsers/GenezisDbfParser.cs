using System;
using System.Data;
using System.Globalization;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class GenezisDbfParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "TRX_NUMBER", "TRX_NUM")
				.DocumentHeader(d => d.DocumentDate, "TRX_DATE")
				.DocumentInvoice(i => i.InvoiceNumber, "TRX_NUM2")
				.DocumentInvoice(i => i.InvoiceNumber, "TRX_DAT2")
				.DocumentInvoice(i => i.BuyerName, "CUSTOMER")
				.DocumentInvoice(i => i.BuyerAddress, "SHIP_TO")
				.Line(l => l.Code, "ITEM_ID")
				.Line(l => l.Product, "ITEM_NAME")
				.Line(l => l.Producer, "VE_NAME", "VEND_NAME")
				.Line(l => l.Country, "VE_COUNTRY", "COUNTRY", "STRANA")
				.Line(l => l.Quantity, "QNTY")
				.Line(l => l.SupplierCost, "PRICE_TAX")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE")
				.Line(l => l.SupplierPriceMarkup, "PER_MARKUP")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE_VR")
				.Line(l => l.RegistryCost, "PRICE_RR", "REESTR", "CENA_REEST")
				.Line(l => l.Nds, "TAX_RATE")
				.Line(l => l.NdsAmount, "TAX_AMOUNT")
				.Line(l => l.Amount, "FULL_AMNT")
				.Line(l => l.SerialNumber, "LOT_NUMBER")
				.Line(l => l.Period, "EXP_DATE")
				.Line(l => l.Certificates, "CER_NUMBER")
				.Line(l => l.EAN13, "EAN13", "SCAN_CODE", "SHTRIH_KOD")
				.Line(l => l.VitallyImportant, "GNVLS", "IS_LIFE")
				.Line(l => l.BillOfEntryNumber, "DECL_NUM");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return (data.Columns.Contains("TRX_NUMBER") || data.Columns.Contains("TRX_NUM"))
				&& data.Columns.Contains("TRX_DATE")
				&& data.Columns.Contains("ITEM_ID")
				&& data.Columns.Contains("ITEM_NAME")
				&& data.Columns.Contains("QNTY")
				&& data.Columns.Contains("TAX_RATE");
		}
	}
}