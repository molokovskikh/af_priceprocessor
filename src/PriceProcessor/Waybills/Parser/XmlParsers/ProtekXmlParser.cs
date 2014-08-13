using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.XmlParsers
{
	public class ProtekXmlParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var xdocument = XDocument.Load(file);
			document.ProviderDocumentId = xdocument.XPathSelectElement("КоммерческаяИнформация/Документ/Номер").Value;
			var docDate = xdocument.XPathSelectElement("КоммерческаяИнформация/Документ/Дата").Value;
			if (!String.IsNullOrEmpty(docDate))
				document.DocumentDate = Convert.ToDateTime(docDate);
			foreach (var element in xdocument.XPathSelectElements("КоммерческаяИнформация/Документ/Товары/Товар")) {
				var line = document.NewLine();
				line.Product = (string)element.XPathSelectElement("Наименование");
				line.Producer = (string)element.XPathSelectElement("Изготовитель/ОфициальноеНаименование");
				line.Code = (string)element.XPathSelectElement("Ид");
				line.Country = (string)element.XPathSelectElement("Страна");
				line.SupplierCost = element.Get("ЦенаЗаЕдиницу");
				line.ProducerCostWithoutNDS = element.Get("ЗначенияСвойств/ЗначенияСвойства[Ид='NAKLBD_PRDPRCWONDS']/Значение");
				line.SerialNumber = ((string)element
					.XPathSelectElement("ЗначенияСвойств/ЗначенияСвойства[Ид='NAKLBD_SERIA']/Значение") ?? "")
					.Split('^').FirstOrDefault();
				line.Quantity = (uint?)element.XPathSelectElement("Количество");
				line.VitallyImportant = (int?)element.XPathSelectElement("ЗначенияСвойств/ЗначенияСвойства[Ид='NAKLBD_VITAL_MED']/Значение") == 1;
				line.Nds = (uint?)element.Get("СтавкиНалогов/СтавкаНалога[Наименование='НДС']/Ставка");
				document.Lines.Add(line);
			}
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			var document = XDocument.Load(file);
			return document.XPathSelectElement("КоммерческаяИнформация") != null;
		}
	}

	public static class Extentions
	{
		public static decimal? Get(this XElement element, string selector)
		{
			var el = element.XPathSelectElement(selector);
			if (el== null)
				return null;
			return Convert.ToDecimal(el.Value, CultureInfo.InvariantCulture);
		}
	}
}