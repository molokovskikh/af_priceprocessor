using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class BellaDonParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NDOC")
				.DocumentHeader(h => h.DocumentDate, "DATEDOC")
				.Line(l => l.Code, "CODEPST")
				.Line(l => l.Product, "NAME")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.Country, "CNTR")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Period, "GDATE")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.SupplierCost, "PRICENDS")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.BillOfEntryNumber, "NUMGTD")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "SERTDATE")
				.Line(l => l.DateOfManufacture, "DATEMADE")
				.Line(l => l.SerialNumber, "SER")

				.Invoice(i => i.InvoiceNumber, "BILLNUM")
				.Invoice(i => i.RecipientName, "CLIENT")
				.Invoice(i => i.InvoiceDate, "BILLDT");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			var CodeIndex = data.Columns.Contains("CODEPST");
			var ProductIndex = data.Columns.Contains("NAME");
			var SupplierCostIndex = data.Columns.Contains("PRICENDS");
			var SupplierCostWithoutNdsIndex = data.Columns.Contains("PRICE");
			var NdsIndex = data.Columns.Contains("NDS");
			var RecipientNameIndex = data.Columns.Contains("CLIENT");



			if (!CodeIndex || !ProductIndex || !RecipientNameIndex)
				return false;

			if (SupplierCostIndex && SupplierCostWithoutNdsIndex)
				return true;
			if (SupplierCostIndex && NdsIndex)
				return true;
			if (SupplierCostWithoutNdsIndex && NdsIndex)
				return true;
			return false;
		}
	}
}
