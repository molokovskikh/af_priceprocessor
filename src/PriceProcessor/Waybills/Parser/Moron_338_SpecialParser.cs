using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	// Отдельный парсер для челябинского Морона (код 338)
	// (вообще-то формат тот же что и у SiaParser, но в колонке PRICE цена БЕЗ Ндс)
	public class Moron_338_SpecialParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);
			string vitallyImportantColumn = null;
			string registryCostColumn = null;

			if (data.Columns.Contains("ZHNVLS"))
				vitallyImportantColumn = "ZHNVLS";
			else if (data.Columns.Contains("ISZHVP"))
				vitallyImportantColumn = "ISZHVP";
			else if (data.Columns.Contains("ISZNVP"))
				vitallyImportantColumn = "ISZNVP";
			else if (data.Columns.Contains("JNVLS"))
				vitallyImportantColumn = "JNVLS";
			else if (data.Columns.Contains("GZWL"))
				vitallyImportantColumn = "GZWL";

			if (data.Columns.Contains("REESTR"))
				registryCostColumn = "REESTR";
			else if (data.Columns.Contains("OTHER"))
				registryCostColumn = "OTHER";

			document.Lines = data.Rows.Cast<DataRow>().Select(r =>
			{
				document.ProviderDocumentId = r["NUM_DOC"].ToString();
				if (!Convert.IsDBNull(r["DATE_DOC"]))
					document.DocumentDate = Convert.ToDateTime(r["DATE_DOC"]);
				var line = document.NewLine();
				line.Code = r["CODE_TOVAR"].ToString();
				line.Product = r["NAME_TOVAR"].ToString();
				line.Producer = r["PROIZ"].ToString();
				line.Country = r["COUNTRY"].ToString();
				line.ProducerCost = Convert.IsDBNull(r["PR_PROIZ"]) ? null : (decimal?)Convert.ToDecimal(r["PR_PROIZ"], CultureInfo.InvariantCulture);
				line.SupplierCostWithoutNDS = Convert.ToDecimal(r["PRICE"], CultureInfo.InvariantCulture);
				if (data.Columns.Contains("NACENKA"))
					line.SupplierPriceMarkup = Convert.IsDBNull(r["NACENKA"]) ? null : (decimal?)Convert.ToDecimal(r["NACENKA"], CultureInfo.InvariantCulture);
				line.Quantity = Convert.ToUInt32(r["VOLUME"]);
				line.Period = Convert.IsDBNull(r["SROK"]) ? null : Convert.ToDateTime(r["SROK"]).ToShortDateString();
				if (data.Columns.Contains("OTHER") && r["OTHER"] != DBNull.Value)
					if (!String.IsNullOrEmpty(registryCostColumn))
						line.RegistryCost = Convert.IsDBNull(r[registryCostColumn]) ? null :
							(decimal?)Convert.ToDecimal(r[registryCostColumn], CultureInfo.InvariantCulture);
				line.Certificates = Convert.IsDBNull(r["DOCUMENT"]) ? null : r["DOCUMENT"].ToString();
				line.SerialNumber = Convert.IsDBNull(r["SERIA"]) ? null : r["SERIA"].ToString();
				line.SetSupplierCostByNds(Convert.ToDecimal(r["PCT_NDS"], CultureInfo.InvariantCulture));
				if (!String.IsNullOrEmpty(vitallyImportantColumn))
					line.VitallyImportant = Convert.IsDBNull(r[vitallyImportantColumn]) ? null : (bool?)(Convert.ToUInt32(r[vitallyImportantColumn]) == 1);
				if (data.Columns.Contains("PR_REG"))
					line.RegistryCost = Convert.IsDBNull(r["PR_REG"]) ? null : (decimal?)Convert.ToDecimal(r["PR_REG"], CultureInfo.InvariantCulture);
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
