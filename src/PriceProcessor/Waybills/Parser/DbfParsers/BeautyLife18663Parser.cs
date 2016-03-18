using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;
using System.Text.RegularExpressions;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class BeautyLife18663Parser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		protected Regex re = new Regex(@"(?<producer>.+)\s/\s(?<product>.+)");

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			if (document.Invoice == null)
				document.SetInvoice();

			document.Lines = data.Rows.Cast<DataRow>().Select(r => {
				document.ProviderDocumentId = r["NOMNAKL"].ToString();
				if (!Convert.IsDBNull(r["DATANAKL"]))
					document.DocumentDate = Convert.ToDateTime(r["DATANAKL"]);
				document.Invoice.BuyerName = r["KONTR"].ToString();

				var line = document.NewLine();
				line.Quantity = Convert.ToUInt32(r["KOL"]);
				line.Code = r["KOD1C"].ToString();
				line.SupplierCost = Convert.ToDecimal(r["PRICE"], CultureInfo.InvariantCulture);
				line.NdsAmount = Convert.ToDecimal(r["NDS"], CultureInfo.InvariantCulture);
				line.Country = r["PROIZV"].ToString();
				line.Certificates = r["SERT"].ToString();
				line.Amount = Convert.ToDecimal(r["SUMMMA"], CultureInfo.InvariantCulture);
				line.SetNds(Convert.ToDecimal(r["STNDS"], CultureInfo.InvariantCulture));

				// NAME - Производитель " / " Название товара (первый слеш с пробелами с двух сторон является разделяющим символом)
				line.Product = re.Match(r["NAME"].ToString()).Groups["product"].Value;
				line.Producer = re.Match(r["NAME"].ToString()).Groups["producer"].Value;

				return line;
			}).ToList();
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("SUMMMA") &&
				data.Columns.Contains("NOMNAKL") &&
				data.Columns.Contains("BNDS") &&
				data.Columns.Contains("DATANAKL");
		}
	}
}
