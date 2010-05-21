using System;
using System.Data;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class FarmGroupParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "DOCNAME")
				.DocumentHeader(h => h.DocumentDate, "DATEDOC")
				.Line(l => l.Product, "GOOD")
				.Line(l => l.Producer, "ENTERP")
				.Line(l => l.Code, "CODE")
				.Line(l => l.SerialNumber, "SERIAL")
				.Line(l => l.Period, "DATEB")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.Quantity, "QUANT")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.SupplierPriceMarkup, "MARGIN")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.SupplierCostWithoutNDS, "PRICEWONDS")
				.Line(l => l.ProducerCost, "PRICEENT")
				.Line(l => l.VitallyImportant, "PV")
				.ToDocument(document, Dbf.Load(file));
			return document;
		}

		public static bool CheckFileFormat(DataTable table)
		{
			return table.Columns.Contains("DATEB")
				&& table.Columns.Contains("SERT");
		}
	}
}