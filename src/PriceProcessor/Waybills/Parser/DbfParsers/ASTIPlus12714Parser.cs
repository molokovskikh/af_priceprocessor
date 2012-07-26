using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class ASTIPlus12714Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NOMDOC")
				.DocumentHeader(d => d.DocumentDate, "DATDOC")
				.DocumentInvoice(i => i.BuyerAddress, "TO")
				.Line(l => l.Code, "CodeTov")
				.Line(l => l.Product, "TovName")
				.Line(l => l.Producer, "PrName")
				.Line(l => l.Country, "PrStrana")
				.Line(l => l.Unit, "EdIzm")
				.Line(l => l.Quantity, "Kol")
				.Line(l => l.SupplierCostWithoutNDS, "CwoNDS")
				.Line(l => l.SupplierCost, "CwNDS")
				.Line(l => l.ProducerCostWithoutNDS, "CPwoNDS")
				.Line(l => l.ProducerCost, "CPwNDS")
				.Line(l => l.Nds, "StNDS")
				.Line(l => l.NdsAmount, "SumNDS")
				.Line(l => l.Amount, "Vsego")
				.Line(l => l.Period, "SrokGodn")
				.Line(l => l.SerialNumber, "Seriya")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.Certificates, "SertNom")
				.Line(l => l.CertificatesDate, "SertData")
				.Line(l => l.CertificateAuthority, "SertOrg")
				.Line(l => l.RegistryCost, "Creestr")
				.Line(l => l.VitallyImportant, "GN2")
				.Line(l => l.EAN13, "EAN");
		}
		public static bool CheckFileFormat(DataTable data)
		{
			var columns = data.Columns;
			return columns.Contains("PrStrana")
				&& columns.Contains("EdIzm")
				&& columns.Contains("CwoNDS")
				&& columns.Contains("CwNDS")
				&& columns.Contains("CPwoNDS")
				&& columns.Contains("CPwNDS")
				&& columns.Contains("StNDS")
				&& columns.Contains("Creestr")
				&& columns.Contains("DECLARANT");
		}
	}
}
