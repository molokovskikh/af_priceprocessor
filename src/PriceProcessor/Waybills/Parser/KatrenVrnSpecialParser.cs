using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class KatrenVrnSpecialParser : IDocumentParser
	{
		public static DataTable Load(string file)
		{
			try {
				return Dbf.Load(file);
			}
			catch (DbfException) {
				return Dbf.Load(file, Encoding.GetEncoding(866), true, false);
			}
		}

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding.GetEncoding(866), true, false);
			new DbfParser()
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "GOOD")
				.Line(l => l.SerialNumber, "SERIAL")
				.Line(l => l.Period, "DATEB")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.Quantity, "QUANT")
				.Line(l => l.SupplierPriceMarkup, "MARGIN")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.VitallyImportant, "JVLS")
				.Line(l => l.Producer, "PRODUCER")
				.ToDocument(document, data);
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("CODE")
				&& data.Columns.Contains("GOOD")
				&& data.Columns.Contains("SERIAL")
				&& data.Columns.Contains("DATEB")
				&& data.Columns.Contains("PRICE")
				&& data.Columns.Contains("QUANT")
				&& data.Columns.Contains("MARGIN")
				&& data.Columns.Contains("NDS")
				&& data.Columns.Contains("REESTR")
				&& data.Columns.Contains("SERT")
				&& data.Columns.Contains("JVLS")
				&& data.Columns.Contains("PRODUCER ");
		}
	}
}