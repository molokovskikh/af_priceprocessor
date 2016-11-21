using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class Flagrans19777Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "N_NACL")
				.DocumentHeader(h => h.DocumentDate, "D_NACL")
				.Invoice(i => i.Amount, "SUMMA_ALL")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "GOOD")
				.Line(l => l.Quantity, "QUANT")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.NdsAmount, "NDS_SUM")
				.Line(l => l.SupplierCost, "PRICE_S_N", "PR_SUPWN")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE", "PRICE_SUP")
				.Line(l => l.Amount, "SUMMA_N")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.RegistryCost, "PRICE_REE")
				.Line(l => l.Certificates, "SERT", "SERIAL")
				.Line(l => l.Period, "DATEB")
				.Line(l => l.Producer, "PRODUCER")
				.Line(l => l.EAN13, "BARCODE")
				.Line(l => l.Country, "COUNTRY");
		}

		public static bool CheckFileFormat(DataTable table)
		{
			return table.Columns.Contains("N_NACL") && table.Columns.Contains("D_NACL")
			       && (table.Columns.Contains("PRICE_S_N") || table.Columns.Contains("PR_SUPWN"));
		}
	}
}

