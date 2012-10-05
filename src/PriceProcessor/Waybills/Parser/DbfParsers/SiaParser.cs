using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser.Helpers;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class SiaParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file);
			string vitallyImportantColumn = null;
			string certificatesColumn = null;
			string certificatesDateColumn = null;
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
			else if (data.Columns.Contains("Priznak_pr"))
				vitallyImportantColumn = "Priznak_pr";
			else if (data.Columns.Contains("VITAL"))
				vitallyImportantColumn = "VITAL";
			else if (data.Columns.Contains("GVLS"))
				vitallyImportantColumn = "GVLS";

			if (data.Columns.Contains("REESTR"))
				registryCostColumn = "REESTR";
			else if (data.Columns.Contains("PR_REG"))
				registryCostColumn = "PR_REG";
			else if (data.Columns.Contains("PRICE_RR"))
				registryCostColumn = "PRICE_RR";
			else if (data.Columns.Contains("OTHER"))
				registryCostColumn = "OTHER";
			if (data.Columns.Contains("cach_reest"))
				registryCostColumn = "cach_reest";

			if (data.Columns.Contains("DOCUMENT"))
				certificatesColumn = "DOCUMENT";
			else if (data.Columns.Contains("CER_NUMBER"))
				certificatesColumn = "CER_NUMBER";

			if (data.Columns.Contains("REG_DATE"))
				certificatesDateColumn = "REG_DATE";


			document.Lines = data.Rows.Cast<DataRow>().Select(r => {
				document.ProviderDocumentId = r["NUM_DOC"].ToString();
				if (!Convert.IsDBNull(r["DATE_DOC"]))
					document.DocumentDate = Convert.ToDateTime(r["DATE_DOC"]);
				var line = document.NewLine();
				line.Code = r["CODE_TOVAR"].ToString();
				line.Product = r["NAME_TOVAR"].ToString();
				line.Producer = r["PROIZ"].ToString();
				line.Country = r["COUNTRY"].ToString();
				line.ProducerCostWithoutNDS = ParseHelper.GetDecimal(r["PR_PROIZ"].ToString());

				if (data.Columns.Contains("SUMMA_NDS"))
					line.NdsAmount = ParseHelper.GetDecimal(r["SUMMA_NDS"].ToString());

				if (data.Columns.Contains("PRICE_NDS") && data.Columns.Contains("PRICE")) {
					line.SupplierCost = ParseHelper.GetDecimal(r["PRICE_NDS"].ToString());
					line.SupplierCostWithoutNDS = ParseHelper.GetDecimal(r["PRICE"].ToString());
				}
				else if ((data.Columns.Contains("BARCODE") && !data.Columns.Contains("srok_prep"))
					|| data.Columns.Contains("PR_S_NDS") && data.Columns.Contains("PRICE") && data.Columns.Contains("GVLS"))
					line.SupplierCostWithoutNDS = Convert.ToDecimal(r["PRICE"], CultureInfo.InvariantCulture);
				else
					line.SupplierCost = Convert.ToDecimal(r["PRICE"], CultureInfo.InvariantCulture);

				if (data.Columns.Contains("NACENKA"))
					line.SupplierPriceMarkup = ParseHelper.GetDecimal(r["NACENKA"].ToString());
				line.Quantity = Convert.ToUInt32(r["VOLUME"]);
				line.Period = Convert.IsDBNull(r["SROK"]) ? null : Convert.ToDateTime(r["SROK"]).ToShortDateString();

				if (!String.IsNullOrEmpty(registryCostColumn))
					line.RegistryCost = ParseHelper.GetDecimal(r[registryCostColumn].ToString());

				if (!String.IsNullOrEmpty(certificatesColumn))
					line.Certificates = ParseHelper.GetString(r[certificatesColumn].ToString());

				if (!String.IsNullOrEmpty(certificatesDateColumn)) {
					if (Convert.IsDBNull(r[certificatesDateColumn]))
						line.CertificatesDate = null;
					else {
						DateTime? dt = ParseHelper.GetDateTime(r[certificatesDateColumn].ToString());
						line.CertificatesDate = (dt == null ? null : dt.Value.ToShortDateString());
					}
				}

				line.SerialNumber = Convert.IsDBNull(r["SERIA"]) ? null : r["SERIA"].ToString();
				if (!Convert.IsDBNull(r["PCT_NDS"]))
					line.SetNds(Convert.ToDecimal(r["PCT_NDS"], CultureInfo.InvariantCulture));

				if (!String.IsNullOrEmpty(vitallyImportantColumn))
					line.VitallyImportant = ParseHelper.GetBoolean(r[vitallyImportantColumn].ToString());
				line.SetAmount();
				line.SetNdsAmount();
				return line;
			}).ToList();
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("CODE_TOVAR") &&
				data.Columns.Contains("NAME_TOVAR") &&
				data.Columns.Contains("PROIZ") &&
				data.Columns.Contains("COUNTRY") &&
				data.Columns.Contains("PR_PROIZ") &&
				data.Columns.Contains("PCT_NDS") &&
				!data.Columns.Contains("EAN13");
		}
	}
}