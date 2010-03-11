using System;
using System.Data;
using System.Linq;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class SiaParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file);
			document.DocumentLines = data.Rows.Cast<DataRow>().Select(r => {
				var line = document.NewLine();
				line.Code = r["CODE_TOVAR"].ToString();
				line.Product = r["NAME_TOVAR"].ToString();
				line.Producer = r["PROIZ"].ToString();
				line.Country = r["COUNTRY"].ToString();
				line.ProducerCost = Convert.ToDecimal(r["PR_PROIZ"]);
				line.SupplierCost = Convert.ToDecimal(r["PRICE"]);
				line.SupplierPriceMarkup = Convert.ToDecimal(r["NACENKA"]);
				line.Quantity = Convert.ToUInt32(r["VOLUME"]);
				line.SetNds(Convert.ToDecimal(r["PCT_NDS"]));
				return line;
			}).ToList();
			return document;
		}
	}
}
