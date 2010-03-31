using System;
using System.Data;
using System.Globalization;
using System.Linq;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class SiaParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file);
			string vitallyImportantColumn = null;
			if (data.Columns.Contains("ZHNVLS"))
				vitallyImportantColumn = "ZHNVLS";
			else if (data.Columns.Contains("ISZHVP"))
				vitallyImportantColumn = "ISZHVP";

			document.Lines = data.Rows.Cast<DataRow>().Select(r => {
				document.ProviderDocumentId = r["NUM_DOC"].ToString();
				var line = document.NewLine();
				line.Code = r["CODE_TOVAR"].ToString();
				line.Product = r["NAME_TOVAR"].ToString();
				line.Producer = r["PROIZ"].ToString();
				line.Country = r["COUNTRY"].ToString();
				line.ProducerCost = Convert.ToDecimal(r["PR_PROIZ"], CultureInfo.InvariantCulture);
				line.SupplierCost = Convert.ToDecimal(r["PRICE"], CultureInfo.InvariantCulture);
				line.SupplierPriceMarkup = Convert.ToDecimal(r["NACENKA"], CultureInfo.InvariantCulture);
				line.Quantity = Convert.ToUInt32(r["VOLUME"]);
				line.Period = Convert.ToDateTime(r["SROK"]).ToShortDateString();
				line.Certificates = r["DOCUMENT"].ToString();
				line.SetNds(Convert.ToDecimal(r["PCT_NDS"], CultureInfo.InvariantCulture));
				if (!String.IsNullOrEmpty(vitallyImportantColumn))
					line.VitallyImportant = Convert.ToUInt32(r[vitallyImportantColumn]) == 1;
				return line;
			}).ToList();
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			var data = Dbf.Load(file);
			return data.Columns.Contains("CODE_TOVAR") &&
				   data.Columns.Contains("NAME_TOVAR") &&
				   data.Columns.Contains("PROIZ") &&
				   data.Columns.Contains("COUNTRY") &&
				   data.Columns.Contains("PR_PROIZ") &&
				   data.Columns.Contains("PCT_NDS");
		}
	}
}
