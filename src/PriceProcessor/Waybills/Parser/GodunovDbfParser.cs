using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class GodunovDbfParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "DOK")
				.DocumentHeader(h => h.DocumentDate, "DATE")
				.Line(l => l.Code, "IDNOM")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "NAME_PRO")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.ProducerCost, "PRICE_ZI")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.Quantity, "KOL")
				.Line(l => l.Period, "SROK")
				.Line(l => l.RegistryCost, "PRICE_REG")
				.Line(l => l.Certificates, "NOM_SERT")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.VitallyImportant, "ISLIVE")
				.Line(l => l.Nds, "NDS")
				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			var data = Dbf.Load(file);
			return data.Columns.Contains("DOK") &&
				   data.Columns.Contains("IDNOM") &&
				   data.Columns.Contains("NAME") &&
				   data.Columns.Contains("NAME_PRO") &&
				   data.Columns.Contains("COUNTRY") &&
				   data.Columns.Contains("PRICE_ZI") &&
				   data.Columns.Contains("ISLIVE") &&
				   data.Columns.Contains("NOM_SERT") &&
				   data.Columns.Contains("KOL");
		}
	}
}
