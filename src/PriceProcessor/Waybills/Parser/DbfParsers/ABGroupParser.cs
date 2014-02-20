using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class ABGroupParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NOM")
				.DocumentHeader(h => h.DocumentDate, "DATA")
				.Invoice(i => i.RecipientAddress, "INFORM")
				.Invoice(i => i.RecipientName, "CONTRAGENT")
				.Invoice(i => i.BuyerINN, "INN")
				.Invoice(i => i.BuyerAddress, "INFORM")
				.Invoice(i => i.BuyerName, "CONTRAGENT")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.SerialNumber, "SERIYA")
				.Line(l => l.Period, "SROK")
				.Line(l => l.SupplierCost, "CENA")
				.Line(l => l.SupplierCostWithoutNDS, "CENABEZNDS")
				.Line(l => l.Amount, "TOTAL")
				.Line(l => l.NdsAmount, "NDS")
				.Line(l => l.Quantity, "KOL");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NOM") &&
				data.Columns.Contains("DATA") &&
				data.Columns.Contains("KOL") &&
				data.Columns.Contains("NAME") &&
				data.Columns.Contains("KOD") &&
				data.Columns.Contains("CENA") &&
				data.Columns.Contains("CENABEZNDS");
		}
	}
}