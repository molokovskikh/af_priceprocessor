namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class ForaFarmParser : BaseDbfParser2
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "DCODE")
				.DocumentHeader(d => d.DocumentDate, "DATE_DOC")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "PREP_NAME")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.Producer, "MANUF_N")
				.Line(l => l.Country, "COUNTRY_N")
				.Line(l => l.EAN13, "SCANDOC")
				.Line(l => l.Certificates, "SERT_N")
				.Line(l => l.CertificatesDate, "SER_SROK")
				.Line(l => l.CertificateAuthority, "ORGAN_SERT")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.Period, "DATE_REES")
				.Line(l => l.SupplierCost, "PRICE_OLP")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.Amount, "SUM_OPL")
				.Line(l => l.NdsAmount, "NDS_SUM")
				.Line(l => l.Nds, "NDS_PR");
		}
	}
}