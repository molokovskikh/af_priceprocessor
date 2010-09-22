using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class MoronKazanParser : BaseDbfParser
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
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.SupplierCostWithoutNDS, "PRICMNDS")
				.Line(l => l.SupplierCost, "PRICWNDS")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.Period, "SROKGODN")
				.Line(l => l.SupplierPriceMarkup, "OPT_NAC")
				.Line(l => l.ProducerCost, "PRICE_PROI")
				.Line(l => l.VitallyImportant, "GNVLS");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("KLI")
				&& data.Columns.Contains("NOMER")
				&& data.Columns.Contains("DATA")
				&& data.Columns.Contains("KOD")
				&& data.Columns.Contains("NM");
		}
	}
}