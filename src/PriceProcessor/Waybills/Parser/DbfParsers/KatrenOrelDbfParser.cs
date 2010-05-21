using System.Data;
using System.Text;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class KatrenOrelDbfParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "DCODE")
				.DocumentHeader(h => h.DocumentDate, "DATE_DOC")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "PRODUCT")
				.Line(l => l.Producer, "PRODUCER")
				.Line(l => l.Country, "")
				.Line(l => l.ProducerCost, "PRO_NNDS")
				.Line(l => l.SupplierCost, "PRICE_OPL")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE_BASE")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.Period, "GOD")
				.Line(l => l.RegistryCost, "PRICE_REES")
				.Line(l => l.Certificates, "SERT_N")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.VitallyImportant, "")
				.Line(l => l.Nds, "NDS_PR")
				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("DCODE") &&
				   data.Columns.Contains("PRICE_BASE") &&
				   data.Columns.Contains("KOLVO") &&
				   data.Columns.Contains("GOD") &&
				   data.Columns.Contains("NDS_PR") &&
				   data.Columns.Contains("PRO_NNDS");
		}
	}
}
