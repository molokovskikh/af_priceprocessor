using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class FlagransParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "N_NAKL")
				.DocumentHeader(h => h.DocumentDate, "D_NAKL")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "GOOD")
				.Line(l => l.Quantity, "QUANT")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.Producer, "PRODUCER")
				.Line(l => l.Period, "DATEB")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.SupplierCost, "PR_SUPWN")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE_PRO")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE_SUP")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.Certificates, "SERIAL")
				.Line(l => l.RegistryCost, "PRICE_REE")

				.Invoice(i => i.Amount, "SUMMA_N")
				.Invoice(i => i.NDSAmount, "NDS_SUM");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			var numNacl = data.Columns.Contains("N_NACL");
			var dateNacl = data.Columns.Contains("D_NACL");
			var codeIndex = data.Columns.Contains("CODE");
			var productIndex = data.Columns.Contains("GOOD");
			var supplierCostIndex = data.Columns.Contains("PR_SUPWN");
			var supplierCostWithoutNdsIndex = data.Columns.Contains("PRICE_SUP");
			var ndsIndex = data.Columns.Contains("NDS");
			var lifeDate = data.Columns.Contains("DATEB");

			return numNacl && dateNacl &&
				codeIndex && productIndex &&
				supplierCostIndex && supplierCostWithoutNdsIndex &&
				ndsIndex && lifeDate;
		}
	}
}
