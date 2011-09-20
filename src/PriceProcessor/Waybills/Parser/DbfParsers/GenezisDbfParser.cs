using System;
using System.Data;
using System.Globalization;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class GenezisDbfParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file);
			if (data.Rows.Count > 0 && !Convert.IsDBNull(data.Rows[0]["TRX_DATE"]))
				document.DocumentDate = Convert.ToDateTime(data.Rows[0]["TRX_DATE"]);

			var documentIdColumn = String.Empty;
			if (data.Columns.Contains("TRX_NUM"))
				documentIdColumn = "TRX_NUM";
			else if (data.Columns.Contains("TRX_NUMBER"))
				documentIdColumn = "TRX_NUMBER";

			var producerColumn = String.Empty;
			if (data.Columns.Contains("VEND_NAME"))
				producerColumn = "VEND_NAME";
			else if (data.Columns.Contains("VE_NAME"))
				producerColumn = "VE_NAME";

			document.Lines = data.Rows.Cast<DataRow>().Select(r =>
			{
				document.ProviderDocumentId = Convert.ToString(r[documentIdColumn], CultureInfo.InvariantCulture);
				var line = document.NewLine();
				line.Code = Convert.ToString(r["ITEM_ID"], CultureInfo.InvariantCulture);
				line.Product = r["ITEM_NAME"].ToString();
				line.Producer = r[producerColumn].ToString();
				if (data.Columns.Contains("VE_COUNTRY"))
					line.Country = r["VE_COUNTRY"].ToString();
				line.ProducerCost = Convert.ToDecimal(r["PRICE_VR"], CultureInfo.InvariantCulture);
				line.SupplierCostWithoutNDS = Convert.ToDecimal(r["PRICE"], CultureInfo.InvariantCulture);
				line.SupplierCost = Convert.ToDecimal(r["PRICE_TAX"], CultureInfo.InvariantCulture);
				line.Quantity = Convert.ToUInt32(r["QNTY"]);
				line.Period = Convert.IsDBNull(r["EXP_DATE"]) ? null : Convert.ToDateTime(r["EXP_DATE"]).ToShortDateString();				
				line.Certificates = r["CER_NUMBER"].ToString();
				line.Nds = Convert.ToUInt32(r["TAX_RATE"], CultureInfo.InvariantCulture);
				line.RegistryCost = Convert.IsDBNull(r["PRICE_RR"]) ? null : (decimal?)Convert.ToDecimal(r["PRICE_RR"]);
				if (data.Columns.Contains("GNVLS"))
					line.VitallyImportant = Convert.IsDBNull(r["GNVLS"]) ? null : (bool?)(Convert.ToUInt32(r["GNVLS"]) == 1);
				line.SerialNumber = r["LOT_NUMBER"].ToString();
				if (data.Columns.Contains("PER_MARKUP"))
					line.SupplierPriceMarkup = Convert.IsDBNull(r["PER_MARKUP"]) ? null : (decimal?)Convert.ToDecimal(r["PER_MARKUP"], CultureInfo.InvariantCulture);
				return line;
			}).ToList();
			return document;
		}

		public static bool CheckFileFormat(DataTable table)
		{
			return table.Columns.Contains("TRX_DATE") &&
				table.Columns.Contains("PRICE_VR") &&
				table.Columns.Contains("QNTY") &&
				table.Columns.Contains("EXP_DATE") &&
				table.Columns.Contains("PRICE") &&
				table.Columns.Contains("PRICE_TAX");			
		}
	}
}
