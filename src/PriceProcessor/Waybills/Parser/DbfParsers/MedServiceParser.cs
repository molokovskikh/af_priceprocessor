using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	/// <summary>
	/// SpecialParser
	/// </summary>
	public class MedServiceParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			var x =  new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "N_NACL")
				.DocumentHeader(h => h.DocumentDate, "D_NACL")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FACTORY")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.Quantity, "QUANTITY")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE_MAKE")
				.Line(l => l.SupplierCost, "PRICE_MAKE")
				.Line(l => l.Amount, "SUM_NAKED")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.Period, "DATE_VALID")
				.Line(l => l.Certificates, "SERT");
			return x;
		}

		public static bool CheckFileFormat(DataTable table)
		{

			var Index = (table.Columns.Contains("N_NACL")) &&
				(table.Columns.Contains("D_NACL")) &&
				table.Columns.Contains("SUM_NAKED") ;
			return Index;
		}
	}
}