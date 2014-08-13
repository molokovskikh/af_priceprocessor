using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Inforoom.PriceProcessor.Waybills.Models;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.Helpers;
using log4net;

namespace Inforoom.PriceProcessor.Waybills.Parser.XmlParsers
{
	public class KrepyshXmlParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var xDocument = XDocument.Load(file);
			var header = xDocument.XPathSelectElement(@"Документ/ЗаголовокДокумента");
			document.ProviderDocumentId = header.XPathSelectElement("НомерДок").Value;

			var docDate = (string)header.XPathSelectElement("ДатаДок");
			if (!String.IsNullOrEmpty(docDate))
				document.DocumentDate = Convert.ToDateTime(docDate);
			else
				document.DocumentDate = DateTime.Now;

			foreach (var position in xDocument.XPathSelectElements(@"Документ/ТоварныеПозиции/ТоварнаяПозиция")) {
				var line = document.NewLine();
				line.Product = (string)position.XPathSelectElement("Товар");
				line.Producer = (string)position.XPathSelectElement("Изготовитель");
				line.Country = (string)position.XPathSelectElement("СтранаИзготовителя");
				line.Code = (string)position.XPathSelectElement("КодТовара");
				line.Quantity = ParseHelper.GetUInt((string)position.XPathSelectElement("Количество"));
				line.SupplierCostWithoutNDS = ParseHelper.GetDecimal((string)position.XPathSelectElement("ЦенаОпт"));
				line.Certificates = (string)position.XPathSelectElement("Серии/Серия/НомерСертиф");
				line.SupplierPriceMarkup = ParseHelper.GetDecimal((string)position.XPathSelectElement("НаценОпт"));
				line.CertificatesDate = (string)position.XPathSelectElement("Серии/Серия/ДатаВыдачиСертиф");
				line.CertificateAuthority = (string)position.XPathSelectElement("Серии/Серия/ОрганСертиф");
				line.ProducerCostWithoutNDS = ParseHelper.GetDecimal((string)position.XPathSelectElement("ЦенаИзг"));
				line.Nds = ParseHelper.GetUInt((string)position.XPathSelectElement("СтавкаНДС"));

				line.Amount = ParseHelper.GetDecimal((string)position.XPathSelectElement("СуммаОптВклНДС"));
				line.NdsAmount = ParseHelper.GetDecimal((string)position.XPathSelectElement("СуммаНДС"));
				line.RetailCost = ParseHelper.GetDecimal((string)position.XPathSelectElement("ЦенаРозн"));
				line.EAN13 = (string)position.XPathSelectElement("ЕАН13");
				line.BillOfEntryNumber = (string)position.XPathSelectElement("ГТД");
				line.Period = (string)position.XPathSelectElement("Серии/Серия/СрокГодностиТовара");
				line.SerialNumber = (string)position.XPathSelectElement("Серии/Серия/СерияТовара");
				line.RegistryCost = ParseHelper.GetDecimal((string)position.XPathSelectElement("ЦенаГР"));

				line.VitallyImportant = ParseHelper.GetBoolean((string)position.XPathSelectElement("ЖВНЛС"));
				if (line.VitallyImportant == null)
					line.VitallyImportant = ParseHelper.GetBoolean((string)position.XPathSelectElement("ЖНВЛС"));
				if (line.RegistryCost > 0)
					line.VitallyImportant = true;
			}
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			try {
				var document = XDocument.Load(file);
				return document.XPathSelectElement("Документ/ЗаголовокДокумента/НомерДок") != null
					&& document.XPathSelectElements("Документ/ТоварныеПозиции/ТоварнаяПозиция").Any()
					&& document.XPathSelectElements("Документ/ТоварныеПозиции/ТоварнаяПозиция").Count() ==
						document.XPathSelectElements("Документ/ТоварныеПозиции/ТоварнаяПозиция/Товар").Count();
			}
			catch (XmlException) {
				return false;
			}
		}
	}
}