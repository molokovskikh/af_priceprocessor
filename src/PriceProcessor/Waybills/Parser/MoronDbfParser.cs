using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class MoronDbfParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file);

			if (data.Rows.Count > 0 && !Convert.IsDBNull(data.Rows[0]["DATAGOT"]))
				document.DocumentDate = Convert.ToDateTime(data.Rows[0]["DATAGOT"]);
			document.Lines = data.Rows.Cast<DataRow>().Select(r =>
			{
				document.ProviderDocumentId = Convert.ToString(r["NUMNAK"], CultureInfo.InvariantCulture);
				var line = document.NewLine();
				line.Code = Convert.ToString(r["KODNLKLEK"], CultureInfo.InvariantCulture);
				line.Product = r["NAMLEK"].ToString();
				line.Producer = r["NAMZAVOD"].ToString();
				line.Country = r["NAMSTRANA"].ToString();
				line.ProducerCost = Convert.ToDecimal(r["CENARAS"], CultureInfo.InvariantCulture);
				line.SupplierCostWithoutNDS = Convert.ToDecimal(r["CENAPRBNDS"], CultureInfo.InvariantCulture);
				line.SupplierCost = Convert.ToDecimal(r["CENAPROD"], CultureInfo.InvariantCulture);
				line.SupplierPriceMarkup = Convert.ToDecimal(r["NACOPT"], CultureInfo.InvariantCulture);
				line.Quantity = Convert.ToUInt32(r["COUNT"]);
				line.Period = Convert.ToDateTime(r["SROKGOD"]).ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("en-US"));				
				line.Certificates = r["NUMBER"].ToString();
				line.SerialNumber = r["SERIJ"].ToString();
				line.Nds = Convert.ToUInt32(r["PRCNDS"], CultureInfo.InvariantCulture);
				line.VitallyImportant = Convert.ToUInt32(r["OBAS"]) == 1;
				return line;
			}).ToList();
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			var table = Dbf.Load(file);
			return table.Columns.Contains("NUMNAK") && 
				table.Columns.Contains("DATAGOT") && 
				table.Columns.Contains("KODAPTEK") &&
				table.Columns.Contains("KODPOSTAV") &&
				table.Columns.Contains("CENAPROD") &&
				table.Columns.Contains("PRCNDS");
		}
	}
}
