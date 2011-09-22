using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class HealthIndustrySmolenskParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NUMBER")
				.DocumentHeader(d => d.DocumentDate, "DATE")

				.Line(l => l.EAN13, "SCANCODE")
				.Line(l => l.Code, "BASECODE")
				.Line(l => l.Product, "LONGNAME")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.Amount, "SUM")
				.Line(l => l.Nds, "STAVNDS")
				.Line(l => l.NdsAmount, "NDS")
				.Line(l => l.SupplierCostWithoutNDS, "PRICEWNDS")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.Period, "GODENDATE")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.BillOfEntryNumber, "NUMGTD")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.Producer, "FACTORY")
				.Line(l => l.VitallyImportant, "GVLS")
				.Line(l => l.RegistryCost, "REGPRICE");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("BASECODE")
				&& data.Columns.Contains("LONGNAME")
				&& data.Columns.Contains("STAVNDS")
				&& data.Columns.Contains("PRICE")
				&& data.Columns.Contains("PRICEWNDS")
				&& data.Columns.Contains("GVLS");
		}
	}
}
