using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class GenesisNNParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "N_NACL")
				.DocumentHeader(h => h.DocumentDate, "D_NACL")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FACTORY")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.Quantity, "QUANTITY")
				.Line(l => l.ProducerCost, "PRICE_MAKE")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE_NAKE")
				.Line(l => l.Nds, "NDS_PR")
				.Line(l => l.RegistryCost, "PRICE_REES")
				.Line(l => l.VitallyImportant, "ISLIFE")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.Certificates, "SERT");
		}

		public static bool CheckFileFormat(DataTable table)
		{
			return table.Columns.Contains("N_NACL") && table.Columns.Contains("D_NACL");
		}
	}
}