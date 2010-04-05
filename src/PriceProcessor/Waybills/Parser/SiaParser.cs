using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class UkonDbfParser : SiaParser
	{
		public UkonDbfParser()
		{
			Encoding = Encoding.GetEncoding(1251);
		}

		public static bool IsInCorrectFileFormat(string file)
		{
			return CheckFileFormat(file) && Dbf.Load(file).Columns.Contains("ISZNVP");
		}
	}

	public class SiaParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);
			string vitallyImportantColumn = null;
			if (data.Columns.Contains("ZHNVLS"))
				vitallyImportantColumn = "ZHNVLS";
			else if (data.Columns.Contains("ISZHVP"))
				vitallyImportantColumn = "ISZHVP";
			else if (data.Columns.Contains("ISZNVP"))
				vitallyImportantColumn = "ISZNVP";

			document.Lines = data.Rows.Cast<DataRow>().Select(r => {
				document.ProviderDocumentId = r["NUM_DOC"].ToString();
				if (!Convert.IsDBNull(r["DATE_DOC"]))
					document.DocumentDate = Convert.ToDateTime(r["DATE_DOC"]);
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
				if (data.Columns.Contains("OTHER") && r["OTHER"] != DBNull.Value)
					line.RegistryCost = Convert.ToDecimal(r["OTHER"], CultureInfo.InvariantCulture);
				line.Certificates = r["DOCUMENT"].ToString();
				line.SerialNumber = r["SERIA"].ToString();
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
