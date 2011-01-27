using System;
using System.Data;
using System.IO;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class FarmGroupParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file);
			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "DOCNAME", "NUM")
				.DocumentHeader(h => h.DocumentDate, "DATEDOC")
				.Line(l => l.Product, "GOOD")
				.Line(l => l.Producer, "ENTERP")
				.Line(l => l.Code, "CODE")
				.Line(l => l.SerialNumber, "SERIAL")
				.Line(l => l.Period, "DATEB", "DETEB")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.Quantity, "QUANT")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.SupplierPriceMarkup, "MARGIN")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.SupplierCostWithoutNDS, "PRICEWONDS")
				.Line(l => l.ProducerCost, "PRICEENT", "PPRICEWT")
				.Line(l => l.VitallyImportant, "PV", "GV", "JVLS", "GNVLS")
				.ToDocument(document, data);
			if (document.ProviderDocumentId.Length > 8
				&& String.Equals(document.ProviderDocumentId.Substring(0, 8), Document.GenerateProviderDocumentId().Substring(0, 8)))
				document.ProviderDocumentId = Path.GetFileNameWithoutExtension(file);
			return document;
		}

		public static bool CheckFileFormat(DataTable table)
		{
			return (table.Columns.Contains("DATEB") || table.Columns.Contains("DETEB"))
				&& table.Columns.Contains("GOOD")
				&& table.Columns.Contains("SERT")
				&& table.Columns.Contains("PRICEWONDS")
				&& (table.Columns.Contains("PV") || table.Columns.Contains("GV") || table.Columns.Contains("JVLS") || table.Columns.Contains("GNVLS"));
		}
	}
}