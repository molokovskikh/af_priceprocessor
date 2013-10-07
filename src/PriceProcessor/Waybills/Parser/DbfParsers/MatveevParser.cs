using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class MatveevParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NDOC")
				.DocumentHeader(h => h.DocumentDate, "DATEDOC")
				.Line(l => l.Code, "CODEPST")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE2N")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.Period, "GDATE")
				.Line(l => l.DateOfManufacture, "DATEMADE")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.Amount, "SUMPAY");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("DATEMADE") &&
				data.Columns.Contains("DATEDOC") &&
				data.Columns.Contains("NDOC") &&
				data.Columns.Contains("NAME") &&
				data.Columns.Contains("FIRM") &&
				data.Columns.Contains("QNT") &&
				data.Columns.Contains("PRICE2N") &&
				!data.Columns.Contains("GNVLS");
		}
	}
}