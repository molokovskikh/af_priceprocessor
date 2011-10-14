using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class FarmakonPlusParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "DOCNUM")
				.DocumentHeader(h => h.DocumentDate, "DOCDATE")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "IZGOT")
				.Line(l => l.Country, "STRANA")
				.Line(l => l.ProducerCostWithoutNDS, "CZAVOD")
				//.Line(l => l.SupplierCost, "COST")
				.Line(l => l.SupplierCostWithoutNDS, "COST")
				.Line(l => l.Quantity, "QTY")
				.Line(l => l.Period, "SROK")
				.Line(l => l.RegistryCost, "CREESTRA")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.SerialNumber, "SERY")
				.Line(l => l.VitallyImportant, "ZHVL")
				.Line(l => l.Nds, "VAT")
				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("DOCNUM") &&
				   data.Columns.Contains("DOCDATE") &&
				   data.Columns.Contains("IZGOT") &&
				   data.Columns.Contains("STRANA") &&
				   data.Columns.Contains("SERY") &&
				   data.Columns.Contains("CZAVOD");
		}
	}
}
