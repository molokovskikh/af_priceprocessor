using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class KosmetikOptParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NOMER")
				.DocumentHeader(d => d.DocumentDate, "DATA")
				.DocumentInvoice(i => i.RecipientAddress, "PARTNAME")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "NAIMPROIZV", "PROIZV")
				.Line(l => l.Period, "SROK")
				.Line(l => l.Quantity, "KOLICH")
				.Line(l => l.Nds, "STNDS")
				.Line(l => l.SupplierCostWithoutNDS, "CENA");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("KOD") &&
				data.Columns.Contains("NAME") &&
				(data.Columns.Contains("NAIMPROIZV") || data.Columns.Contains("PROIZV")) &&
				data.Columns.Contains("KOLICH") &&
				data.Columns.Contains("STNDS") &&
				data.Columns.Contains("CENA") &&
				data.Columns.Contains("DATA") &&
				data.Columns.Contains("NOMER");
		}
	}
}