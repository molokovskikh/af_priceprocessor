using System.Data;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class TredifarmCheboksaryParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file);
			new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "N2")
				.DocumentHeader(h => h.ProviderDocumentId, "N3")
				.Line(l => l.Code, "N4")
				.Line(l => l.Product, "N5")
				.Line(l => l.Producer, "N13")
				.Line(l => l.Country, "N11")
				.Line(l => l.SupplierCostWithoutNDS, "N19")
				.Line(l => l.SupplierCost, "N20")
				.Line(l => l.Quantity, "N21")
				.Line(l => l.ProducerCostWithoutNDS, "N18")
				.Line(l => l.Period, "N16")
				.Line(l => l.Nds, "N7")
				.Line(l => l.Certificates, "N8")
				.Line(l => l.VitallyImportant, "N25")
				.ToDocument(document, data);
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return 
				data.Columns[0].ColumnName=="N3"
				&& data.Columns.Contains("N3")
				&& data.Columns.Contains("N2")
				&& data.Columns.Contains("N20")
				&& data.Columns.Contains("N21")
				&& data.Columns.Contains("N7");
		}
	}
}
