using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class BelaLtdParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(l => l.ProviderDocumentId, "DOCNO")
				.DocumentHeader(l => l.DocumentDate, "DOCDAT")
				.Invoice(i => i.Amount, "SUMMA")
				.Invoice(i => i.NDSAmount, "NDS")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "TOVAR")
				.Line(l => l.Quantity, "KOL")
				.Line(l => l.SupplierCostWithoutNDS, "TZENA")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificateAuthority, "SORG")
				.Line(l => l.CertificatesDate, "SERTOT")
				.Line(l => l.Country, "STRANA")
				.Line(l => l.ProducerCostWithoutNDS, "ZAVOD")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.SupplierCost, "TZENANDS")
				.Line(l => l.NdsAmount, "SUMMANDS")
				.Line(l => l.Nds, "NDSstavk")
				.Line(l => l.VitallyImportant, "PV")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.BillOfEntryNumber, "GTD");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("CODE")
				&& data.Columns.Contains("TOVAR")
				&& data.Columns.Contains("KOL")
				&& data.Columns.Contains("TZENA")
				&& data.Columns.Contains("PROIZV");
		}
	}
}