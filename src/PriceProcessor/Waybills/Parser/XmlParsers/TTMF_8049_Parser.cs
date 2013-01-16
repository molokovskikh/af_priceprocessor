using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.XmlParsers
{
	public class TTMF_8049_Parser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var xdocument = XDocument.Load(file);
			if(document.Invoice == null)
				document.SetInvoice();
			foreach (var element in xdocument.XPathSelectElements("NewDataSet/position")) {
				if(document.ProviderDocumentId == null)
					document.ProviderDocumentId = element.XPathSelectElement("TTN").Value;
				var docDate = element.XPathSelectElement("TTN_DATE") == null ? null : element.XPathSelectElement("TTN_DATE").Value;
				if (!String.IsNullOrEmpty(docDate) && document.DocumentDate == null)
					document.DocumentDate = Convert.ToDateTime(docDate);
				var line = document.NewLine();
				line.Product = element.XPathSelectElement("NAME_POST").Value;
				line.Producer = element.XPathSelectElement("PRZV_POST") == null ? null : element.XPathSelectElement("PRZV_POST").Value;
				line.SerialNumber = element.XPathSelectElement("SERIA") == null ? null : element.XPathSelectElement("SERIA").Value;
				line.Period = element.XPathSelectElement("SGODN") == null ? null : element.XPathSelectElement("SGODN").Value;
				line.Certificates = element.XPathSelectElement("SERT") == null ? null : element.XPathSelectElement("SERT").Value;
				line.CertificatesDate = element.XPathSelectElement("SERT_DATE") == null ? null : element.XPathSelectElement("SERT_DATE").Value;
				line.ProducerCostWithoutNDS = element.XPathSelectElement("PRCENA_BNDS") == null ? null : (decimal?)element.Get("PRCENA_BNDS");
				line.Nds = element.XPathSelectElement("NDS") == null ? null : (uint?)SafeConvert.ToDecimalInvariant(element.XPathSelectElement("NDS").Value);
				line.Cipher = element.XPathSelectElement("SHIFR") == null ? null : element.XPathSelectElement("SHIFR").Value;
				line.TradeCost = element.XPathSelectElement("OPT_PRICE") == null ? null : (decimal?)element.Get("OPT_PRICE");
				line.SaleCost = element.XPathSelectElement("OTP_CENA") == null ? null : (decimal?)element.Get("OTP_CENA");
				line.RetailCost = element.XPathSelectElement("RCENA") == null ? null : (decimal?)element.Get("RCENA");
				line.Quantity = Convert.ToUInt32(element.XPathSelectElement("KOL_TOV").Value);
				if(String.IsNullOrEmpty(document.Invoice.BuyerName))
					document.Invoice.BuyerName = element.XPathSelectElement("AGNABBR") == null ? null : element.XPathSelectElement("AGNABBR").Value;
				if(document.Invoice.BuyerId == null)
					document.Invoice.BuyerId = element.XPathSelectElement("TELEX") == null ? null : (int?)Convert.ToInt32(element.XPathSelectElement("TELEX").Value);
				if(String.IsNullOrEmpty(document.Invoice.StoreName))
					document.Invoice.StoreName = element.XPathSelectElement("STORENAME") == null ? null : element.XPathSelectElement("STORENAME").Value;
			}
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			var document = XDocument.Load(file);
			return document.XPathSelectElement("NewDataSet/position") != null;
		}
	}
}
