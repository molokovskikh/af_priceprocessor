using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class MoronDbfParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NUMNAK")
				.DocumentHeader(d => d.DocumentDate, "DATAGOT")
				.Line(l => l.Code, "KODNLKLEK")
				.Line(l => l.Product, "NAMLEK")
				.Line(l => l.Producer, "NAMZAVOD")
				.Line(l => l.Country, "NAMSTRANA")
				.Line(l => l.Period, "SROKGOD")
				.Line(l => l.SerialNumber, "SERIJ")
				.Line(l => l.Quantity, "COUNT")
				.Line(l => l.SupplierCost, "CENAPROD")
				.Line(l => l.ProducerCostWithoutNDS, "CENARAS")
				.Line(l => l.Nds, "PRCNDS")
				.Line(l => l.VitallyImportant, "OBAS")
				.Line(l => l.RegistryCost, "CENAREE")
				.Line(l => l.Certificates, "NUMBER")
				.Line(l => l.SupplierCostWithoutNDS, "CENAPRBNDS");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NUMNAK") &&
				data.Columns.Contains("DATAGOT") &&
				data.Columns.Contains("KODAPTEK") &&
				data.Columns.Contains("KODPOSTAV") &&
				data.Columns.Contains("CENAPROD") &&
				data.Columns.Contains("PRCNDS");
		}
	}
}