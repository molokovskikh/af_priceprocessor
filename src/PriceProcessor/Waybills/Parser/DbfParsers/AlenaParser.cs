using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class AlenaParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(1251);
		//protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "N_NACL") 
				.DocumentHeader(h => h.DocumentDate, "D_NACL") 
				.Line(l => l.Code, "CODE") 
				.Line(l => l.Product, "GOOD") 
				.Line(l => l.SupplierCost, "PRICE_SUP") 
				.Line(l => l.SupplierCostWithoutNDS, "PRICE_NNDS") 
				.Line(l => l.NdsAmount, "NDS")
				.Line(l => l.Quantity, "QUANT")
				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("N_NACL") &&
				data.Columns.Contains("D_NACL") &&
				data.Columns.Contains("CODE") &&
				data.Columns.Contains("GOOD") &&
				data.Columns.Contains("PRICE_SUP") &&
				data.Columns.Contains("PRICE_NNDS") &&
				data.Columns.Contains("QUANT");
		}
	}
}
