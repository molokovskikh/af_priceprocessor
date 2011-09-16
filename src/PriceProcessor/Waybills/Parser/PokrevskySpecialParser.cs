using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class PokrevskySpecialParser : IDocumentParser
	{
		public static DataTable Load(string file)
		{
			try
			{
				return Dbf.Load(file);
			}
			catch (DbfException)
			{
				return Dbf.Load(file, Encoding.GetEncoding(866), true, false);
			}
		}

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding.GetEncoding(866), true, false);
			new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NUM_DOC")
				.DocumentHeader(d => d.DocumentDate, "DATE_DOC")
				.Line(l => l.Code, "CODE_TOVAR")
				.Line(l => l.Product, "NAME_TOVAR")
				.Line(l => l.Producer, "PROIZ")
				.Line(l => l.Quantity, "VOLUME")
				.Line(l => l.ProducerCost, "PR_PROIZ")
				.Line(l => l.SupplierPriceMarkup, "NACENKA")
				.Line(l => l.Nds, "PCT_NDS")
				.Line(l => l.NdsAmount, "SUMMA_NDS")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE")
				.Line(l => l.Amount, "SUMMA")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.Certificates, "DOCUMENT")
				.ToDocument(document, data);
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NUM_DOC") &&
				data.Columns.Contains("DATE_DOC") &&
				data.Columns.Contains("CODE_TOVAR") &&
				data.Columns.Contains("NAME_TOVAR") &&
				data.Columns.Contains("PROIZ") &&
				data.Columns.Contains("VOLUME") &&
				data.Columns.Contains("PR_PROIZ") &&
				data.Columns.Contains("NACENKA") &&
				data.Columns.Contains("PCT_NDS") &&
				data.Columns.Contains("SUMMA_NDS") &&
				data.Columns.Contains("PRICE");
		}
	}
}
