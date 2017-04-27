using System;
using System.ComponentModel;
using System.Xml.Linq;
using System.Xml.XPath;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.XmlParsers
{
	public class PureProfitParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var xdocument = XDocument.Load(file);
			var order = xdocument.XPathSelectElement("Order");
			document.ProviderDocumentId = (string)order.XPathSelectElement("OrderId");
			document.DocumentDate = NullableConvert.ToDateTime((string)order.XPathSelectElement("OrderDate"));
			foreach (var item in order.XPathSelectElements("Items/Item")) {
				var line = document.NewLine();
				line.Product = (string)item.XPathSelectElement("GoodsName");
				line.Code = (string)item.XPathSelectElement("ItemId");
				line.EAN13 = NullableConvert.ToUInt64((string)item.XPathSelectElement("EAN13"));
				line.Quantity = (uint?)item.XPathSelectElement("Quantity");
				line.SupplierCostWithoutNDS = (decimal?)item.XPathSelectElement("CostClear");
				line.SupplierCost = (decimal?)item.XPathSelectElement("Cost");
				line.Nds = (uint?)item.XPathSelectElement("STNDS");
				line.NdsAmount = (decimal?)item.XPathSelectElement("NDS");
				line.Amount = (decimal?)item.XPathSelectElement("SUMMA");
			}
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			var document = XDocument.Load(file);
			return document.XPathSelectElement("Order/Items/Item") != null;
		}
	}
}