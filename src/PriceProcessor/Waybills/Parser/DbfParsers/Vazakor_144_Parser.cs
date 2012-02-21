using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class Vazakor_144_Parser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			document.Lines = data.Rows.Cast<DataRow>().Select(r => {
				document.ProviderDocumentId = Convert.ToString(r["NUM_DOC"], CultureInfo.InvariantCulture);
				document.DocumentDate = DateTime.Parse(r["DATE_DOC"].ToString());

				var invoice = document.SetInvoice();
				invoice.BuyerName = r["USER"].ToString();
				invoice.SellerName = r["PRODAVEC"].ToString();

				var line = document.NewLine();
				line.Code = r["CODE_TOVAR"].ToString();
				line.Product = r["NAME_TOVAR"].ToString();
				line.Producer = r["PROIZ"].ToString();
				line.SupplierCostWithoutNDS = Convert.ToDecimal(r["CENABEZNDS"], CultureInfo.InvariantCulture);
				line.SupplierCost = Convert.ToDecimal(r["PRICE"], CultureInfo.InvariantCulture);
				line.Amount = Convert.ToDecimal(r["SUMMA"], CultureInfo.InvariantCulture);
				line.NdsAmount = Convert.ToDecimal(r["SUMMA_NDS"], CultureInfo.InvariantCulture);
				line.Quantity = Convert.ToUInt32(r["VOLUME"], CultureInfo.InvariantCulture);
				line.Unit = r["EDINIZM"].ToString();
				line.Certificates = r["DOCUMENT"].ToString();
				line.CertificatesDate = DateTime.Parse(r["SROKSERT"].ToString()).ToShortDateString();
				line.Nds = Convert.ToUInt32(r["PCT_NDS"].ToString().Replace("%", string.Empty), CultureInfo.InvariantCulture);
				return line;
			}).ToList();

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("CODE_TOVAR") &&
				data.Columns.Contains("LINKSERT") &&
				data.Columns.Contains("PCT_NDS") &&
				data.Columns.Contains("EDINIZM") &&
				data.Columns.Contains("PRODAVEC") &&
				data.Columns.Contains("NUM_DOC");
		}
	}
}
