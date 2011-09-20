using System;
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
			foreach (var element in xdocument.XPathSelectElements("КоммерческаяИнформация/Документ/Товары/Товар"))
			{
				var line = document.NewLine();
				line.Product = element.XPathSelectElement("Наименование").Value;
				line.Producer = element.XPathSelectElement("Изготовитель/ОфициальноеНаименование").Value;
				line.Code = element.XPathSelectElement("Ид").Value;
				line.Country = element.XPathSelectElement("Страна").Value;
				line.SupplierCost = element.Get("ЦенаЗаЕдиницу");
				line.ProducerCost = element.Get("ЗначенияСвойств/ЗначенияСвойства[Ид='NAKLBD_PRDPRCWONDS']/Значение");
				var serialNumber = element.XPathSelectElement("ЗначенияСвойств/ЗначенияСвойства[Ид='NAKLBD_SERIA']/Значение").Value;
				if (serialNumber != null)
					line.SerialNumber = serialNumber.Split('^')[0];
				line.Quantity = Convert.ToUInt32(element.XPathSelectElement("Количество").Value);
				line.VitallyImportant = Convert.ToInt32(element.XPathSelectElement("ЗначенияСвойств/ЗначенияСвойства[Ид='NAKLBD_VITAL_MED']/Значение").Value) == 1;
				line.SetNds(element.Get("СтавкиНалогов/СтавкаНалога[Наименование='НДС']/Ставка"));				
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
}
