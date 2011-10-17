using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class AralPlusParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "INVNUM")
				.DocumentHeader(h => h.DocumentDate, "INVDT")
				.Line(l => l.Product, "ITEMID")
				.Line(l => l.Producer, "FIRMID")
				.Line(l => l.Country, "LANDID")
				.Line(l => l.Code, "LOCALCOD")
				.Line(l => l.Quantity, "ITEMQTY")
				.Line(l => l.SupplierCostWithoutNDS, "CATPRNV")
				.Line(l => l.SupplierCost, "CATTOT")
				.Line(l => l.Nds, "VAT")
				.Line(l => l.Period, "USEBEFOR")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.Certificates, "SERNUMID")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.RegistryCost, "REGPR")
				.Line(l => l.ProducerCostWithoutNDS, "PRODPRNV")
				.Line(l => l.SupplierPriceMarkup, "WHLPRUP");
		}

		public static bool CheckFileFormat(DataTable table)
		{
			return table.Columns.Contains("WHLPRUP");
		}
	}
}