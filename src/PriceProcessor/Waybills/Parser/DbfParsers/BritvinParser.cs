using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class BritvinParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "DOCNO")
				.DocumentHeader(h => h.DocumentDate, "DOCDAT")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "TOVAR")
				.Line(l => l.Quantity, "KOL")
				.Line(l => l.SupplierCostWithoutNDS, "TZENA")
				// SUMMA (Сумма без НДС по позиции) игнорируется
				.Line(l => l.NdsAmount, "NDS")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificateAuthority, "SORG")
				.Line(l => l.CertificatesDate, "SERTDO")
				.Line(l => l.Period, "GODEN")
				.Line(l => l.Country, "STRANA")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.SupplierCost, "TZENANDS")
				.Line(l => l.Amount, "SUMMANDS")
				.Line(l => l.Nds, "NDSSTAVK")
				.Line(l => l.EAN13, "BARCODE")
				.Invoice(i => i.Amount, "SUMMANAKL");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			var isProductCode = data.Columns.Contains("CODE");
			var isProductName = data.Columns.Contains("TOVAR");
			var isSupplierCost = data.Columns.Contains("TZENANDS");
			var isSupplierCostWithoutNds = data.Columns.Contains("TZENA");
			var isNds = data.Columns.Contains("NDSSTAVK");

			if (!isProductCode || !isProductName)
				return false;
			if (!isSupplierCost || !isSupplierCostWithoutNds || !isNds)
				return false;

			return (data.Columns.Contains("DOCNO") &&
			        data.Columns.Contains("DOCDAT") &&
			        data.Columns.Contains("KOL") &&
			        data.Columns.Contains("NDS") &&
			        data.Columns.Contains("SERTIF") &&
			        data.Columns.Contains("SERTDO") &&
			        data.Columns.Contains("SUMMANAKL"));
		}
	}
}
