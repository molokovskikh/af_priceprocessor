using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class RynkovParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NDOC")
				.DocumentHeader(h => h.DocumentDate, "DATEDOC")
				.Line(l => l.Code, "CODEPST")
				.Line(l => l.Product, "NAME")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE2N")
				.Line(l => l.SupplierCost, "PRICENDS")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.RegistryDate, "GDATE")
				.Line(l => l.DateOfManufacture, "DATEMADE")
				.Line(l => l.Country, "CNTRMADE")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.BillOfEntryNumber, "NUMGTD")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "SERTDATE")
				.Line(l => l.Period, "GOD_SERT")
				.Line(l => l.CertificateAuthority, "SERTORG")
				.Line(l => l.Amount, "SUMPAY")
				.Line(l => l.NdsAmount, "SUMNDS")
				.Invoice(i => i.Amount, "SUMS0")
				.Invoice(i => i.NDSAmount, "SUMSNDS");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			var isProductCode = data.Columns.Contains("CODEPST");
			var isProductName = data.Columns.Contains("NAME");
			var isSupplierCost = data.Columns.Contains("PRICENDS");
			var isSupplierCostWithoutNds = data.Columns.Contains("PRICE2N");
			var isNds = data.Columns.Contains("NDS");

			if (!isProductCode || !isProductName)
				return false;
			if (!isSupplierCost || !isSupplierCostWithoutNds || !isNds)
				return false;

			return data.Columns.Contains("EAN13") &&
			       data.Columns.Contains("GDATE") &&
			       data.Columns.Contains("QNTPACK") &&
			       data.Columns.Contains("GNVLS") &&
			       data.Columns.Contains("NUMGTD") &&
			       data.Columns.Contains("GOD_SERT") &&
			       !data.Columns.Contains("REGPRC");
		}
	}
}
