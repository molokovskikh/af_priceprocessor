using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class KrasotaIZdorovieKazanParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NOM")
				.DocumentHeader(h => h.DocumentDate, "DATA")

				.DocumentInvoice(i => i.RecipientAddress, "CONTRAGENT")

				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NAME")

				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.SerialNumber, "SERIYA")
				.Line(l => l.Period, "SROK")

				.Line(l => l.SupplierCostWithoutNDS, "CENA")

				.Line(l => l.Amount, "TOTAL")
				.Line(l => l.NdsAmount, "NDS")

				.Line(l => l.Quantity, "KOL")

				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NOM") &&
			       data.Columns.Contains("DATA") &&
			       data.Columns.Contains("CONTRAGENT") &&
			       data.Columns.Contains("TOTAL") &&
			       data.Columns.Contains("NAME") &&
			       data.Columns.Contains("KOD") &&
			       data.Columns.Contains("CENA");
		}
	}
}
