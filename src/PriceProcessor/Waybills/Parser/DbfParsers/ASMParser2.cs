namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class ASMParser2 : BaseDbfParser2
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "DOC_ID")
				.DocumentHeader(h => h.DocumentDate, "DOC_DATE")
				.Invoice(i => i.RecipientAddress, "STAN")
				.Line(l => l.Code, "G_CODE")
				.Line(l => l.Product, "G_NAME")
				.Line(l => l.CodeCr, "M_CODE")
				.Line(l => l.Producer, "M_NAME")
				.Line(l => l.Country, "C_NAME")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.Certificates, "CER_ID")
				.Line(l => l.CertificatesDate, "CER_EXPR")
				.Line(l => l.Quantity, "QUANTITY")
				.Line(l => l.Nds, "PR_NDS")
				.Line(l => l.SupplierCostWithoutNDS, "CENABEZNDS")
				.Line(l => l.SupplierCost, "CENASNDS")
				.Line(l => l.Amount, "SUM");
		}
	}
}