﻿using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class AptekaHoldingParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var providerDocumentIdColumn = "DOCNUMBER";
			var data = Dbf.Load(file);
			if (data.Columns.Contains("DOCNUMDER"))
			{
				data = Dbf.Load(file);
				providerDocumentIdColumn = "DOCNUMDER";
			}

			if (data.Rows.Count > 0 && !Convert.IsDBNull(data.Rows[0]["REGDATE"]))
				document.DocumentDate = Convert.ToDateTime(data.Rows[0]["REGDATE"]);

			document.Lines = data.Rows.Cast<DataRow>().Select(r => {
				document.ProviderDocumentId = Convert.ToString(r[providerDocumentIdColumn], CultureInfo.InvariantCulture);
				var line = document.NewLine();
				line.Code = Convert.ToString(r["GOODSID"], CultureInfo.InvariantCulture);
				line.Product = r["GOODSN"].ToString();
				line.Producer = r["FIRMN"].ToString();
				line.Country = Convert.IsDBNull(r["COUNTRYN"]) ? null : r["COUNTRYN"].ToString();
				line.ProducerCost = Convert.ToDecimal(r["PRICEF"], CultureInfo.InvariantCulture);
				line.SupplierCost = Convert.ToDecimal(r["PRICE"], CultureInfo.InvariantCulture);
				line.Quantity = Convert.ToUInt32(r["QUANTITY"]);
				line.Period = Convert.ToDateTime(r["BESTBEFORE"]).ToShortDateString();
				line.Certificates = Convert.IsDBNull(r["ANALYSIS"]) ? null : r["ANALYSIS"].ToString();
				line.SetNds(Convert.ToUInt32(r["NDS"], CultureInfo.InvariantCulture));
				line.RegistryCost = Convert.IsDBNull(r["PRICEREG"]) ? null : (decimal?) Convert.ToDecimal(r["PRICEREG"]);
				if (data.Columns.Contains("ISDEC"))
					line.VitallyImportant = Convert.IsDBNull(r["ISDEC"]) ? null : (bool?) (Convert.ToUInt32(r["ISDEC"]) == 1);
				line.SerialNumber = r["SERIES"].ToString();
				return line;
			}).ToList();
			return document;
		}

		public static bool CheckFileFormat(DataTable table)
		{
			return table.Columns.Contains("DOCNUMBER") &&
				table.Columns.Contains("REGDATE") &&
				table.Columns.Contains("INN") &&
				table.Columns.Contains("GOODSID") &&
				table.Columns.Contains("PRICE") &&
				table.Columns.Contains("GOODSN") &&
				table.Columns.Contains("FIRMN") &&
				table.Columns.Contains("SERIES");
		}
	}
}
