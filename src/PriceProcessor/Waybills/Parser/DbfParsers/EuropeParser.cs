using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class EuropeParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "OSNOVANIE")
				.DocumentHeader(d => d.DocumentDate, "DATE")
				.Line(l => l.Code, "NNUM")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Unit, "ED")
				.Line(l => l.Quantity, "KOL")
				.Line(l => l.SupplierCostWithoutNDS, "CENA0")
				.Line(l => l.Nds, "NDS_TAX")
				.Line(l => l.NdsAmount, "NDS_SUM")
				.Line(l => l.Amount, "SUMMA")
				.Line(l => l.Certificates, "SERTIFICAT")
				.Line(l => l.CertificatesEndDate, "PRIM")
				.Line(l => l.CertificatesDate, "SERTDATEST")
				.Line(l => l.CertificateAuthority, "SERTORGAN")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.EAN13, "BARCODE")
				.Line(l => l.Producer, "MAKER");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("KONTRAGENT") &&
				data.Columns.Contains("OSNOVANIE") &&
				data.Columns.Contains("CENA0") &&
				data.Columns.Contains("SERTORGAN") &&
				data.Columns.Contains("DATE") &&
				data.Columns.Contains("NAME") &&
				data.Columns.Contains("SERTIFICAT") &&
				!data.Columns.Contains("CODE") &&
				!data.Columns.Contains("DATE2");
		}
	}
}