using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class PrimerFarmaParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser()
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "GOOD")

				.Line(l => l.Producer, "ENTERP")
				.Line(l => l.Country, "COUNTRY")

				.Line(l => l.ProducerCostWithoutNDS, "PRISEENT")
				.Line(l => l.SupplierCostWithoutNDS, "PRICEWONDS")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.RegistryCost, "REESTR")

				.Line(l => l.Quantity, "QUANT")

				.Line(l => l.Period, "DATES")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.SerialNumber, "SERIAL")
				.Line(l => l.VitallyImportant, "PV")

				.Line(l => l.Nds, "NDS")
				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("PRISEENT") &&
				   data.Columns.Contains("PRICEWONDS") &&
				   data.Columns.Contains("QUANT") &&
				   data.Columns.Contains("ENTERP") &&
				   data.Columns.Contains("GOOD") &&
				   !data.Columns.Contains("DATEDOC") &&
				   data.Columns.Contains("PV");
		}
	}
}
