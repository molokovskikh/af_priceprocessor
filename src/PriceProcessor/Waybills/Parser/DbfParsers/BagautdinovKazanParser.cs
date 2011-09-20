using System.Data;
using System.IO;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class BagautdinovKazanParser: BaseDbfParser
	{
		public override Document Parse(string file, Document document)
		{
			document = base.Parse(file, document);
			document.ProviderDocumentId = new FileInfo(file).Name.Replace(".dbf", "");
			return document;
		}

		public override DbfParser GetParser()
		{
			return new DbfParser()
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Quantity, "KOL")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.Certificates, "CERTNUM");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("KOD")
				&& data.Columns.Contains("NAME")
				&& data.Columns.Contains("PRICE")
				&& data.Columns.Contains("NDS")
				&& data.Columns.Contains("KOL")
				&& data.Columns.Contains("CERTNUM");
		}
	}
}
