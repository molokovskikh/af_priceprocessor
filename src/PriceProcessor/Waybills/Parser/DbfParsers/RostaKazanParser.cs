using System.Data;


namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class RostaKazanParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NOMER")
				.DocumentHeader(h => h.DocumentDate, "DATA")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NM")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.VitallyImportant, "ZVNLS")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE_PR")
				.Line(l => l.SupplierCostWithoutNDS, "PRICMNDS")
				.Line(l => l.SupplierCost, "PRICWNDS")
				.Line(l => l.SupplierPriceMarkup, "NAC")
				.Line(l => l.NdsAmount, "SUMMANDS")
				.Line(l => l.Amount, "SUMWONDS")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.Period, "SROKGODN")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificateAuthority, "SERTKEM")
				.Line(l => l.CertificatesDate, "SERTDATA")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.DocumentInvoice(i => i.BuyerName, "KLI");
		}

		public static bool CheckFileFormat(DataTable table)
		{
			return table.Columns.Contains("KLI")
				&& table.Columns.Contains("NOMER")
				&& table.Columns.Contains("NM")
				&& table.Columns.Contains("REESTR")
				&& table.Columns.Contains("PRICE_PR")
				&& table.Columns.Contains("PRICMNDS")
				&& table.Columns.Contains("SROKGODN")
				&& table.Columns.Contains("SERTIF")
				&& table.Columns.Contains("ZVNLS");
		}
	}
}
