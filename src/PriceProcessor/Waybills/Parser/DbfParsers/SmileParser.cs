using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	// Парсер для накладной поставщика Смайл (требование 3663)
	public class SmileParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "DOCNUM")
				.DocumentHeader(d => d.DocumentDate, "DOCDATE")
				.Line(l => l.Product, "WARESNAME")
				.Line(l => l.Code, "WARESCODE")
				.Line(l => l.Producer, "PRODNAME")
				.Line(l => l.Country, "COUNTRYNAM")
				.Line(l => l.Quantity, "AMOUNT")
				.Line(l => l.SupplierCostWithoutNDS, "PRICEOPT")
				.Line(l => l.ProducerCostWithoutNDS, "PRICEPROD")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.Certificates, "CERTNUM")
				.Line(l => l.Period, "WARESVALID")

				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.CertificateAuthority, "CERTORG")
				.Line(l => l.CertificatesDate, "CERTDATE")
				.Invoice(i => i.BuyerName, "BUYERNAME")
				.Invoice(i => i.BuyerId, "BUYERCODE")
				.Invoice(i => i.SellerName, "SUPNAME")
				.Invoice(i => i.RecipientAddress, "CARGERNAME", "COMMENT");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("WARESNAME")
				&& data.Columns.Contains("WARESCODE")
				&& data.Columns.Contains("PRODNAME")
				&& data.Columns.Contains("WARESVALID");
		}
	}
}