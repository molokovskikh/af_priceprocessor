using System;
using System.Globalization;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class SiaXmlParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var xDocument = XDocument.Load(file);
			document.ProviderDocumentId = xDocument.XPathSelectElement("Документ/ЗаголовокДокумента/НомерДок").Value;
			foreach(var position in xDocument.XPathSelectElements(@"Документ/ТоварныеПозиции/ТоварнаяПозиция"))
			{
				var line = document.NewLine();
				line.Product = position.XPathSelectElement("Товар").Value;
				line.Producer = position.XPathSelectElement("Изготовитель").Value;
				line.Quantity = Convert.ToUInt32(position.XPathSelectElement("Количество").Value);
				line.ProducerCost = Convert.ToDecimal(position.XPathSelectElement("ЦенаИзг").Value, CultureInfo.InvariantCulture);
				line.RegistryCost = Convert.ToDecimal(position.XPathSelectElement("ЦенаГР").Value, CultureInfo.InvariantCulture);
				line.SupplierPriceMarkup = Convert.ToDecimal(position.XPathSelectElement("НаценОпт").Value, CultureInfo.InvariantCulture);
				line.SupplierCost = position.Get("ЦенаОпт");
				line.SetNds(position.Get("СтавкаНДС"));
				line.VitallyImportant = Convert.ToInt32(position.XPathSelectElement("ЖНВЛС").Value) == 1;
				line.Period = position.XPathSelectElement("Серии/Серия/СрокГодностиТовара").Value;
			}
			return document;
		}

		public bool IsInCorrectFormat(string file)
		{
			var document = XDocument.Load(file);
			return document.XPathSelectElement("Документ/ЗаголовокДокумента/ТипДок") != null;
		}
	}

	public static class Extentions
	{
		public static decimal Get(this XElement element, string selector)
		{
			return Convert.ToDecimal(element.XPathSelectElement(selector).Value, CultureInfo.InvariantCulture);
		}
	}
}
