using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using System.IO;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class KatrenVrnMusatovParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file);
			var fileName = Path.GetFileNameWithoutExtension(file);
			document.ProviderDocumentId = fileName;
			new DbfParser()
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "GOODE")
				.Line(l => l.SerialNumber, "SERIAL")
				.Line(l => l.Period, "DATEB")
				.Line(l => l.SupplierCostWithoutNDS, "PPRICEWT")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.Quantity, "QUANT")
				.Line(l => l.SupplierPriceMarkup, "MARGIN")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.Producer, "PRODUSER")
				.Line(l => l.ProducerCost, "PPRICENDS")
				.Line(l => l.VitallyImportant, "GV")
				.ToDocument(document, data);
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("CODE")
				&& data.Columns.Contains("GOODE")
				&& data.Columns.Contains("SERIAL")
				&& data.Columns.Contains("PPRICEWT")
				&& data.Columns.Contains("PRICE")
				&& data.Columns.Contains("PRODUSER");
		}
	}
}
