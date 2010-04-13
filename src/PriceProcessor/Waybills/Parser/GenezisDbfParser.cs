using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class GenezisDbfParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file);
			if (data.Rows.Count > 0 && !Convert.IsDBNull(data.Rows[0]["TRX_DATE"]))
				document.DocumentDate = Convert.ToDateTime(data.Rows[0]["TRX_DATE"]);
			document.Lines = data.Rows.Cast<DataRow>().Select(r =>
			{
				document.ProviderDocumentId = Convert.ToString(r["TRX_NUM"], CultureInfo.InvariantCulture);
				var line = document.NewLine();
				line.Code = Convert.ToString(r["ITEM_ID"], CultureInfo.InvariantCulture);
				line.Product = r["ITEM_NAME"].ToString();
				line.Producer = r["VEND_NAME"].ToString();
				if (data.Columns.Contains("VE_COUNTRY"))
					line.Country = r["VE_COUNTRY"].ToString();
				line.ProducerCost = Convert.ToDecimal(r["PRICE_VR"], CultureInfo.InvariantCulture);
				line.SupplierCostWithoutNDS = Convert.ToDecimal(r["PRICE"], CultureInfo.InvariantCulture);
				line.SupplierCost = Convert.ToDecimal(r["PRICE_TAX"], CultureInfo.InvariantCulture);
				line.Quantity = Convert.ToUInt32(r["QNTY"]);
				line.Period = Convert.ToDateTime(r["EXP_DATE"]).ToShortDateString();
				line.Certificates = r["CER_NUMBER"].ToString();
				line.Nds = Convert.ToUInt32(r["TAX_RATE"], CultureInfo.InvariantCulture);
				line.RegistryCost = Convert.IsDBNull(r["PRICE_RR"]) ? null : (decimal?)Convert.ToDecimal(r["PRICE_RR"]);
				if (data.Columns.Contains("GNVLS"))
					line.VitallyImportant = Convert.IsDBNull(r["GNVLS"]) ? null : (bool?)(Convert.ToUInt32(r["GNVLS"]) == 1);
				line.SerialNumber = r["LOT_NUMBER"].ToString();
				return line;
			}).ToList();
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			var table = Dbf.Load(file);
			return table.Columns.Contains("TRX_DATE") &&
				table.Columns.Contains("TRX_NUM") &&
				table.Columns.Contains("CER_NUMBER") &&
				table.Columns.Contains("DUE_DATE") &&
				table.Columns.Contains("PRICE") &&
				table.Columns.Contains("PRICE_TAX");			
		}
	}
}
