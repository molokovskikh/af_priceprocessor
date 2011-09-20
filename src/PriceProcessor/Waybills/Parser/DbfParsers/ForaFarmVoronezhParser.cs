using System;
using System.Data;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class ForaFarmVoronezhParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file);
			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "DOCNUMBER")
				.DocumentHeader(d => d.DocumentDate, "DATENUMBER")
				.Line(l => l.Product, "GOODSNAME")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.Quantity, "QUANTITY")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.RegistryCost, "PRICEGRESS")
				.Line(l => l.Period, "DATELIFE") //
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.Producer, "FIRMNAME")
				.Line(l => l.Certificates, "NUMCERT")
				.ToDocument(document, data);
			return document;
		}

		public static bool CheckFileFormat(DataTable table)
		{
			return table.Columns.Contains("GOODSNAME")
			       && table.Columns.Contains("DATELIFE")
			       && table.Columns.Contains("FIRMNAME")
			       //&& table.Columns.Contains("CODP")
			       && table.Columns.Contains("NUMCERT");
			//&& table.Columns.Contains("GNVLS")
			//&& table.Columns.Contains("SER");
		}
	}
}
