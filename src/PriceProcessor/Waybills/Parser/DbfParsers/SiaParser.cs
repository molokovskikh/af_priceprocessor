using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class SiaParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			var supplierCostColumn = "PRICE_NDS";
			var supplierCostWithoutNdsColumn = "PRICE";
			if (Data.Columns.Contains("PRICE_NDS") && Data.Columns.Contains("PRICE")) {
				supplierCostColumn = "PRICE_NDS";
				supplierCostWithoutNdsColumn = "PRICE";
			}
			else if ((Data.Columns.Contains("BARCODE") && !Data.Columns.Contains("srok_prep"))
				|| Data.Columns.Contains("PR_S_NDS") && Data.Columns.Contains("PRICE") && Data.Columns.Contains("GVLS"))
				supplierCostWithoutNdsColumn = "PRICE";
			else {
				supplierCostColumn = "PRICE";
				supplierCostWithoutNdsColumn = "dummy";
			}

			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NUM_DOC")
				.DocumentHeader(d => d.DocumentDate, "DATE_DOC")
				.Invoice(i => i.InvoiceNumber, "NUM_SF")
				.Invoice(i => i.InvoiceDate, "DATE_SF")
				.Invoice(i => i.ShipperInfo, "ORG")
				.Invoice(i => i.BuyerName, "POLUCH")
				.Line(l => l.Code, "CODE_TOVAR")
				.Line(l => l.Product, "NAME_TOVAR")
				.Line(l => l.Producer, "PROIZ")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.ProducerCostWithoutNDS, "PR_PROIZ")
				.Line(l => l.SupplierCost, supplierCostColumn)
				.Line(l => l.SupplierCostWithoutNDS, supplierCostWithoutNdsColumn)
				.Line(l => l.SupplierPriceMarkup, "NACENKA")
				.Line(l => l.Quantity, "VOLUME")
				.Line(l => l.Amount, "SUM_B_NDS")
				.Line(l => l.Period, "SROK")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.Nds, "PCT_NDS")
				.Line(l => l.NdsAmount, "SUMMA_NDS")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.RegistryCost, "REESTR", "PR_REG", "PRICE_RR", "OTHER", "cach_reest")
				.Line(l => l.VitallyImportant,
					"ZHNVLS", "ISZHVP", "ISZNVP", "JNVLS", "GZWL", "Priznak_pr", "VITAL", "GVLS", "GNVLS", "GV")
				.Line(l => l.Certificates, "DOCUMENT", "CER_NUMBER")
				.Line(l => l.CertificatesDate, "REG_DATE")
				.Line(l => l.CertificateAuthority, "SERT_ORG")
				.Line(l => l.EAN13, "EAN13", "BARCODE");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("CODE_TOVAR") &&
				data.Columns.Contains("NAME_TOVAR") &&
				data.Columns.Contains("PROIZ") &&
				data.Columns.Contains("COUNTRY") &&
				data.Columns.Contains("PR_PROIZ") &&
				data.Columns.Contains("PCT_NDS");
		}
	}
}