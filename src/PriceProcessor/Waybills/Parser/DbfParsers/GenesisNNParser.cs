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
				.Invoice(i => i.BuyerName, "APTEKA")
				.Invoice(i => i.ShipperInfo, "FILIAL")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FACTORY")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.Quantity, "QUANTITY")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE_MAKE")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE_NAKE")
				.Line(l => l.Nds, "NDS_PR")
				.Line(l => l.RegistryCost, "PRICE_REES")
				.Line(l => l.VitallyImportant, "ISLIFE")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.Period, "DATE_VALID")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.NdsAmount, "NDS_SUM")
				.Line(l => l.Amount, "SUM", "SUM_NAKED")
				.Line(l => l.Nds, "NDS_PR")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.EAN13, "SCANCOD")
				.Line(l => l.SupplierPriceMarkup, "SUM_MARGIN");
		}

		public static bool CheckFileFormat(DataTable table)
		{
			return table.Columns.Contains("N_NACL") && table.Columns.Contains("D_NACL");
		}
	}
}