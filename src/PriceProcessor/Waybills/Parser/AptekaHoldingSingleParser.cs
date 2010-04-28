﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class AptekaHoldingSingleParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			string vitallyImportantColumn = null;
			string certificatesColumn = null;
			string registryCostColumn = null;
			string ndsColumn = null;

			var data = Dbf.Load(file, Encoding);
			if (data.Columns.Contains("PV"))
				vitallyImportantColumn = "PV";

			if (data.Columns.Contains("REG"))
				registryCostColumn = "REG";

			if (data.Columns.Contains("SERTIF"))
				certificatesColumn = "SERTIF";

			if (data.Columns.Contains("NDSstavk"))
				ndsColumn = "NDSstavk";
			else if (data.Columns.Contains("NDSstavk"))
				ndsColumn = "NDSSTAVK";

			document.Lines = data.Rows.Cast<DataRow>().Select(r =>
			{
				document.ProviderDocumentId = Convert.ToString(r["DOCNO"]);
				if (!Convert.IsDBNull(r["DOCDAT"]))
					document.DocumentDate = Convert.ToDateTime(r["DOCDAT"]);
				var line = document.NewLine();
				line.Code = r["CODE"].ToString();
				line.Product = r["TOVAR"].ToString();
				line.Producer = r["PROIZV"].ToString();
				line.Country = r["STRANA"].ToString();
				line.ProducerCost = Convert.IsDBNull(r["ZAVOD"]) ? null : (decimal?)Convert.ToDecimal(r["ZAVOD"], CultureInfo.InvariantCulture);
				line.SupplierCostWithoutNDS = Convert.ToDecimal(r["TZENA"], CultureInfo.InvariantCulture);
				line.SupplierCost = Convert.ToDecimal(r["TZENANDS"], CultureInfo.InvariantCulture);
				line.Quantity = Convert.ToUInt32(r["KOL"]);
				line.Period = Convert.IsDBNull(r["GODEN"]) ? null : Convert.ToDateTime(r["GODEN"]).ToShortDateString();

				if (!String.IsNullOrEmpty(registryCostColumn))
					line.RegistryCost = Convert.IsDBNull(r[registryCostColumn]) ? null :
						(decimal?)Convert.ToDecimal(r[registryCostColumn], CultureInfo.InvariantCulture);

				if (!String.IsNullOrEmpty(certificatesColumn))
					line.Certificates = Convert.IsDBNull(r[certificatesColumn]) ? null : r[certificatesColumn].ToString();

				line.SerialNumber = Convert.IsDBNull(r["SERIA"]) ? null : r["SERIA"].ToString();
				line.Nds = Convert.IsDBNull(r[ndsColumn]) ? null : (uint?)Convert.ToUInt32(r[ndsColumn], CultureInfo.InvariantCulture);
				if (!String.IsNullOrEmpty(vitallyImportantColumn))
					line.VitallyImportant = Convert.IsDBNull(r[vitallyImportantColumn]) ? null : (bool?)(Convert.ToUInt32(r[vitallyImportantColumn]) == 1);
				return line;
			}).ToList();
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			var data = Dbf.Load(file);
			return data.Columns.Contains("DOCNO") &&
				   data.Columns.Contains("TOVAR") &&
				   data.Columns.Contains("CODE") &&
				   data.Columns.Contains("PROIZV") &&
				   data.Columns.Contains("TZENANDS") &&
				   data.Columns.Contains("GODEN") &&
				   data.Columns.Contains("KOL") &&
				   data.Columns.Contains("DOCDAT");
		}
	}
}
