namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class YarFarmParser : BaseDbfParser2
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.DocumentDate, "DOCDATE")
				.DocumentHeader(h => h.ProviderDocumentId, "DOCNUMBER")
				.Invoice(i => i.NDSAmount, "SUMITOGNDS")
				.Invoice(i => i.Amount, "SOITOGWNDS")
				.Invoice(i => i.RecipientId, "KODCLIENT")
				.Invoice(i => i.RecipientName, "NAMECLIENT")
				.Invoice(i => i.RecipientAddress, "ADRCLIENT")
				.Line(l => l.Code, "TOVCOD", "TOVCODE")
				.Line(l => l.Product, "TOVNAME")
				.Line(l => l.CodeCr, "FABRCOD")
				.Line(l => l.Producer, "FABRNAME")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.Quantity, "QUANTITY", "KOL")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.Period, "SROKG")
				.Line(l => l.BillOfEntryNumber, "NUMBERGTD")
				.Line(l => l.Certificates, "CERTIF")
				.Line(l => l.CertificatesDate, "CERTIFDATE")
				.Line(l => l.CertificateAuthority, "CERTIFLAB")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.ProducerCostWithoutNDS, "PFABRNONDS")
				.Line(l => l.SupplierCostWithoutNDS, "POPTNONDS")
				.Line(l => l.SupplierCost, "POPTWNDS")
				.Line(l => l.SupplierPriceMarkup, "POPTNAC")
				.Line(l => l.Amount, "SOPTWNDS")
				.Line(l => l.NdsAmount, "SUMNDS")
				.Line(l => l.RetailCost, "RPRICE")
				.Line(l => l.EAN13, "BARCODE")
				.Line(l => l.VitallyImportant, "JV");
		}
	}
}