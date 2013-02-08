﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	// Отдельный парсер для челябинского Морона (код 338)
	// (вообще-то формат тот же что и у SiaParser, но в колонке PRICE цена БЕЗ Ндс)
	public class Moron_338_SpecialParser : IDocumentParser
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
			var data = Load(file);
			string vitallyImportantColumn = null;
			string certificatesColumn = null;
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
			else if (data.Columns.Contains("GV"))
				vitallyImportantColumn = "GV";

			if (data.Columns.Contains("REESTR"))
				registryCostColumn = "REESTR";
			else if (data.Columns.Contains("OTHER"))
				registryCostColumn = "OTHER";
			else if (data.Columns.Contains("PR_REG"))
				registryCostColumn = "PR_REG";
			else if (data.Columns.Contains("PRICE_RR"))
				registryCostColumn = "PRICE_RR";
			else if (data.Columns.Contains("REESTRPRIC"))
				registryCostColumn = "REESTRPRIC";
			if (data.Columns.Contains("cach_reest"))
				registryCostColumn = "cach_reest";

			if (data.Columns.Contains("DOCUMENT"))
				certificatesColumn = "DOCUMENT";
			else if (data.Columns.Contains("CER_NUMBER"))
				certificatesColumn = "CER_NUMBER";

			document.Lines = data.Rows.Cast<DataRow>().Select(r => {
				document.ProviderDocumentId = r["NUM_DOC"].ToString();
				if (!Convert.IsDBNull(r["DATE_DOC"]))
					document.DocumentDate = Convert.ToDateTime(r["DATE_DOC"]);
				var line = document.NewLine();
				line.Code = r["CODE_TOVAR"].ToString();
				line.Product = r["NAME_TOVAR"].ToString();
				line.Producer = r["PROIZ"].ToString();
				line.Country = r["COUNTRY"].ToString();
				line.ProducerCostWithoutNDS = Convert.IsDBNull(r["PR_PROIZ"])
					? null
					: (decimal?)Convert.ToDecimal(r["PR_PROIZ"], CultureInfo.InvariantCulture);
				line.SupplierCostWithoutNDS = Convert.ToDecimal(r["PRICE"], CultureInfo.InvariantCulture);
				if (data.Columns.Contains("NACENKA"))
					line.SupplierPriceMarkup = Convert.IsDBNull(r["NACENKA"])
						? null
						: (decimal?)Convert.ToDecimal(r["NACENKA"], CultureInfo.InvariantCulture);
				line.Quantity = Convert.ToUInt32(r["VOLUME"]);
				line.Period = Convert.IsDBNull(r["SROK"]) ? null : Convert.ToDateTime(r["SROK"]).ToShortDateString();

				if (!String.IsNullOrEmpty(certificatesColumn))
					line.Certificates = Convert.IsDBNull(r[certificatesColumn]) ? null : r[certificatesColumn].ToString();
				line.SerialNumber = Convert.IsDBNull(r["SERIA"]) ? null : r["SERIA"].ToString();
				line.SetSupplierCostByNds(Convert.ToDecimal(r["PCT_NDS"], CultureInfo.InvariantCulture));
				if (!String.IsNullOrEmpty(vitallyImportantColumn))
					line.VitallyImportant = Convert.IsDBNull(r[vitallyImportantColumn])
						? null
						: (bool?)(Convert.ToUInt32(r[vitallyImportantColumn]) == 1);
				if (!String.IsNullOrEmpty(registryCostColumn))
					line.RegistryCost = Convert.IsDBNull(r[registryCostColumn]) ? null : (decimal?)Convert.ToDecimal(r[registryCostColumn], CultureInfo.InvariantCulture);
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
				data.Columns.Contains("VOLUME");
		}
	}
}