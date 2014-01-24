namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class OriolaParser : BaseDbfParser2
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NAKLNUM")
				.DocumentHeader(h => h.DocumentDate, "NAKLDATE")
				.Line(l => l.Code, "PREPCODE")
				.Line(l => l.EAN13, "BARCODE")
				.Line(l => l.Product, "PREPNAME")
				.Line(l => l.Quantity, "QNTY")
				.Line(l => l.NdsAmount, "NDSSUMSTR")
				.Line(l => l.Amount, "SUMSTR_NDS")
				.Line(l => l.SupplierCost, "SALEPRCNDS")
				.Line(l => l.SupplierCostWithoutNDS, "SALEPRC")
				.Line(l => l.Producer, "MNFT")
				.Line(l => l.CodeCr, "MNFTCODE")
				.Line(l => l.ProducerCost, "MNFTPRCNDS")
				.Line(l => l.ProducerCostWithoutNDS, "MNFTPRC")
				.Line(l => l.Country, "CNTRY")
				.Line(l => l.CountryCode, "CNTRYCODE")
				.Line(l => l.SerialNumber, "SRS")
				.Line(l => l.VitallyImportant, "VITALLOG")
				.Line(l => l.Certificates, "CRT")
				.Line(l => l.CertificatesDate, "CRTBGNDT")
				.Line(l => l.CertificateAuthority, "CRTORG")
				.Line(l => l.DateOfManufacture, "SRSEXPDATE")
				.Line(l => l.RegistryDate, "REGDT")
				.Line(l => l.Period, "SRSMNFDATE")
				.Line(l => l.RegistryCost, "REGPRC");
		}
	}
}