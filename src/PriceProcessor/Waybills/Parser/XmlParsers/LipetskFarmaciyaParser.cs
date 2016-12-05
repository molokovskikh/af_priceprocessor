using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.XmlParsers
{
	public class LipetskFarmaciyaParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var xdocument = XDocument.Load(file);
			var xdocHeader = xdocument.XPathSelectElement("Документы/Документ");
			document.ProviderDocumentId = xdocument.XPathSelectElement("Документы/Документ").Attribute("НомерДокумента")?.Value;
			var docDate = xdocument.XPathSelectElement("Документы/Документ").Attribute("ДатаДокумента")?.Value;
			if (!String.IsNullOrEmpty(docDate))
				document.DocumentDate = Convert.ToDateTime(docDate);
			foreach (var element in xdocument.XPathSelectElements("Документы/Документ/СтрокаДокумента"))
			{
				var line = document.NewLine();
				line.Product = (string) element.Attribute("НоменклатураНаименованиеПолное");
				line.Producer = (string) element.Attribute("Производитель");
				line.Country = (string) element.Attribute("Страна");

				//var culture = CultureInfo.CreateSpecificCulture("fr-FR");
				line.SupplierCost = decimal.Parse((string)element.Attribute("ЦенаРозничная"));
				line.ProducerCostWithoutNDS = decimal.Parse((string)element.Attribute("ЦенаПроизводителяБезНДС"));

				line.SerialNumber = (string)element.Attribute("Серия");
				line.Quantity = (uint?) element.Attribute("Количество");
				line.VitallyImportant = (element.Attribute("ЖНВП")?.Value == "Да");
				var nds = element.Attribute("СтавкаНДС")?.Value.Replace("%", "");
				line.Nds = uint.Parse(nds);
				document.Lines.Add(line);
			}
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			var document = XDocument.Load(file);
			return document.XPathSelectElement("Документы/Документ/СтрокаДокумента") != null;
		}
	}
}