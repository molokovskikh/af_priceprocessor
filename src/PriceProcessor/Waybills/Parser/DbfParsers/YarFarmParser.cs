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
				.Invoice(i => i.BuyerId, "BRANCHID")
				.Invoice(i => i.BuyerName, "BRANCH")
				.Invoice(i => i.SellerName, "DISTR")
				.Invoice(i => i.SellerINN, "DISTRINN")
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
				.Line(l => l.ProducerCost, "PFABRWNDS")
				.Line(l => l.ProducerCostWithoutNDS, "PFABRNONDS")
				.Line(l => l.SupplierCostWithoutNDS, "POPTNONDS")
				.Line(l => l.SupplierCost, "POPTWNDS")
				.Line(l => l.SupplierPriceMarkup, "POPTNAC")
				.Line(l => l.Amount, "SOPTWNDS")
				.Line(l => l.NdsAmount, "SUMNDS")
				// RetailCost - розничная цена, RPRICE - цена реестра 
				// http://redmine.analit.net/issues/39453 "розничной цене, которая отображается в программе, то там поле рассчитывается"
				//.Line(l => l.RetailCost, "RPRICE")
				.Line(l => l.EAN13, "BARCODE", "BARCODE1")
				.Line(l => l.RegistryCost, "RPRICE")
				.Line(l => l.VitallyImportant, "JV", "NEEDASSORT");

			// остатки по задаче http://redmine.analit.net/issues/40037
			//.Line(l => l.RetailCost, "PROZNWNDS"); // продажная цена
			//.Invoice(i => i., "DISTRID")  // код поставщика
			//.Line(l => l., "REGNUMBER")   // Номер регистрации
			//.Line(l => l., "SOPTNONDS")   // Сумма без НДС


		}
	}
}