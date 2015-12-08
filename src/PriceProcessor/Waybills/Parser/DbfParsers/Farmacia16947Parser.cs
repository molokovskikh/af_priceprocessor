using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class Farmacia16947Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			var parcer = new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "DOCDATE")
				.DocumentHeader(d => d.ProviderDocumentId, "NDOC")
				.Invoice(i => i.InvoiceDate, "SF_DATE")
				.Invoice(i => i.InvoiceNumber, "SF_NUMB")
				.Invoice(i => i.RecipientAddress, "ADDRESS")
				//.Invoice(i => i.RecipientId, "GRUZ_RN") больше int
				.Invoice(i => i.RecipientName, "AGN_DEST")
				.Line(l => l.Amount, "SUMMWNDS")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.CertificateAuthority, "SERT_AGN")
				.Line(l => l.Certificates, "SERT_NUMB")
				.Line(l => l.CertificatesDate, "SERT_FROM")
				.Line(l => l.CertificatesEndDate, "SERT_TO")
				.Line(l => l.Code, "SNOMMODIF")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.EAN13, "BARCODE")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.NdsAmount, "NDS_SUMM2")
				.Line(l => l.Period, "SROK_GODN")
				.Line(l => l.Producer, "PRODUCER")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE_PROD")
				.Line(l => l.Product, "NAME_LS")
				.Line(l => l.Quantity, "KOL_LS")
				.Line(l => l.RegistryCost, "REESTRUB")
				.Line(l => l.SerialNumber, "SERNUMB")
				.Line(l => l.SupplierCost, "PRICEWNDS")
				.Line(l => l.SupplierCostWithoutNDS, "PRICEWONDS")
				.Line(l => l.Unit, "ED_IZM")
				.Line(l => l.VitallyImportant, "GVN");

			return parcer;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			var result = false;
			if (data.Columns.Contains("NAME_LS")
					&& data.Columns.Contains("PRODUCER")
					&& data.Columns.Contains("KOL_LS")
					&& data.Columns.Contains("PRICEWONDS")
					&& data.Columns.Contains("NNOMMODIF")
					&& data.Columns.Contains("GV_NAC_SUM")
					&& data.Columns.Contains("GRUZ_RN"))
				result = true;
			return result;
		}
	}
}