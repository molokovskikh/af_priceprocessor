﻿using System;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class ProtekXmlParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var xdocument = XDocument.Load(file);
			document.ProviderDocumentId = xdocument.XPathSelectElement("КоммерческаяИнформация/Документ/Номер").Value;
			foreach (var element in xdocument.XPathSelectElements("КоммерческаяИнформация/Документ/Товары/Товар"))
			{
				var line = document.NewLine();
				line.Product = element.XPathSelectElement("Наименование").Value;
				line.Producer = element.XPathSelectElement("Изготовитель/ОфициальноеНаименование").Value;
				line.Code = element.XPathSelectElement("Ид").Value;
				line.Country = element.XPathSelectElement("Страна").Value;
				line.SupplierCost = element.Get("ЦенаЗаЕдиницу");
				line.Quantity = Convert.ToUInt32(element.XPathSelectElement("Количество").Value);
				line.VitallyImportant = Convert.ToInt32(element.XPathSelectElement("ЗначенияСвойств/ЗначенияСвойства[Ид='NAKLBD_VITAL_MED']/Значение").Value) == 1;
				line.SetNds(element.Get("СтавкиНалогов/СтавкаНалога[Наименование='НДС']/Ставка"));
				document.Lines.Add(line);
			}
			return document;
		}

		public bool IsInCorrectFormat(string file)
		{
			var document = XDocument.Load(file);
			return document.XPathSelectElement("КоммерческаяИнформация") != null;
		}
	}
}
