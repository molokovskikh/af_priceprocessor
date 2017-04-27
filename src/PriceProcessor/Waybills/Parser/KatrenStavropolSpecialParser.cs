using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Inforoom.PriceProcessor.Waybills.Models;
using System.IO;
using System.Text;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser.XmlParsers
{
	public class KatrenStavropolSpecialParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var sb = new StringBuilder();
			using (var sr = new StreamReader(file, Encoding.GetEncoding(1251))) {
				while (sr.Peek() >= 0) {
					var line = sr.ReadLine();
					if (!String.IsNullOrEmpty(line) && line.TrimStart().StartsWith("<"))
						sb.Append(line);
				}
			}

			var xdocument = XDocument.Parse(sb.ToString());
			if(document.Invoice == null)
				document.SetInvoice();

			var header = xdocument.XPathSelectElement("DOCUMENTS/DOCUMENT/HEADER");
			document.Invoice.Amount = GetDecimal(header, "doc_sum");
			document.Invoice.SellerName = header.Attribute("firm_name").Value;
			document.Invoice.BuyerName = header.Attribute("client_name").Value;
			document.ProviderDocumentId = header.Attribute("doc_number").Value;
			document.DocumentDate = GetDate(header, "doc_date");
			//header.Attribute("pay_date").Value;

			foreach (var element in xdocument.XPathSelectElements("DOCUMENTS/DOCUMENT/DETAIL")) {
				var line = document.NewLine();

				line.EAN13 = NullableConvert.ToUInt64(element.Attribute("ean13_code").Value);
				line.Code = element.Attribute("tov_code").Value;
				line.Product = element.Attribute("tov_name").Value;
				line.Producer = element.Attribute("maker_name").Value;
				//line. = element.Attribute("tov_godn").Value; Срок годности
				line.SerialNumber = element.Attribute("tov_seria").Value;
				line.Quantity = (uint?)GetDecimal(element, "kolvo");
				line.ProducerCostWithoutNDS = GetDecimal(element, "maker_price");
				line.RegistryCost = GetDecimal(element, "reestr_price");
				line.SupplierCostWithoutNDS = GetDecimal(element, "firm_price");
				line.NdsAmount = GetDecimal(element, "firm_nds");
				//line. = GetDecimal(element, "firm_nac");// Наценка
				line.Country = element.Attribute("gtd_country").Value;
				line.BillOfEntryNumber = element.Attribute("gtd_number").Value;
				//line. = element.Attribute("sert_number").Value; // Номер сертификата
				line.CertificatesEndDate = GetDate(element, "sert_godn");
			}
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			var result = false;
			using (var sr = new StreamReader(file)) {
				var line = sr.ReadLine();
				if (!String.IsNullOrEmpty(line) && line.StartsWith("$DOCUMENT"))
					result = true;
			}
			return result;
		}

		private decimal? GetDecimal(XElement el, string attrName)
		{
			decimal result;
			var style = NumberStyles.AllowDecimalPoint;
			var culture = CultureInfo.InvariantCulture;

			var attr = el.Attribute(attrName);
			if (attr != null
				&& !String.IsNullOrEmpty(attr.Value)
				&& Decimal.TryParse(attr.Value, style, culture, out result)) {
				return result;
			}
			else
				return null;
		}

		private DateTime? GetDate(XElement el, string attrName)
		{
			DateTime result;
			var attr = el.Attribute(attrName);
			if (attr != null
				&& !String.IsNullOrEmpty(attr.Value)
				&& DateTime.TryParse(attr.Value, out result)) {
				return result;
			}
			else
				return null;
		}

	}
}