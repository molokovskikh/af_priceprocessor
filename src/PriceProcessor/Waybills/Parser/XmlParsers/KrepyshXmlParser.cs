﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using Inforoom.PriceProcessor.Waybills.Models;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser.XmlParsers
{
	public class KrepyshXmlParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var xDocument = XDocument.Load(file);
			bool hasOpt = xDocument.Descendants(@"ТоварнаяПозиция").Descendants("НаценОпт").Any();
			bool hasCode = xDocument.Descendants(@"ТоварнаяПозиция").Descendants("КодТовара").Any();
			bool hasSerialNum = xDocument.Descendants(@"Серия").Descendants("СерияТовара").Any();
			bool hasRegCost = xDocument.Descendants(@"ТоварнаяПозиция").Descendants("ЦенаГР").Any();
			bool hasProducerCost = xDocument.Descendants(@"ТоварнаяПозиция").Descendants("ЦенаИзг").Any();
			bool hasNds = xDocument.Descendants(@"ТоварнаяПозиция").Descendants("СтавкаНДС").Any();
			
			var header = xDocument.XPathSelectElement(@"Документ/ЗаголовокДокумента");
			document.ProviderDocumentId = header.XPathSelectElement("НомерДок").Value;
			var docDate = header.XPathSelectElement("ДатаДок").Value;
			if (!String.IsNullOrEmpty(docDate))
				document.DocumentDate = Convert.ToDateTime(docDate);

			foreach (var position in xDocument.XPathSelectElements(@"Документ/ТоварныеПозиции/ТоварнаяПозиция")) {
				var line = document.NewLine();
				line.Product = position.XPathSelectElement("Товар").Value;
				line.Producer = position.XPathSelectElement("Изготовитель").Value;
				if (position.XPathSelectElement("СтранаИзготовителя") != null)
					line.Country = position.XPathSelectElement("СтранаИзготовителя").Value;
				if (hasCode)
					line.Code = (position.XPathSelectElement("КодТовара") == null) ? null : position.XPathSelectElement("КодТовара").Value;
				line.Quantity = UInt32.Parse(position.XPathSelectElement("Количество").Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
				if (!hasOpt || String.IsNullOrEmpty(position.XPathSelectElement("НаценОпт").Value))
					line.SupplierPriceMarkup = null;
				else
					line.SupplierPriceMarkup = SafeConvert.ToDecimalInvariant(position.XPathSelectElement("НаценОпт").Value);
				line.SupplierCostWithoutNDS = position.Get("ЦенаОпт");
				line.Certificates = position.XPathSelectElement("Серии/Серия/НомерСертиф").Value;

				if (xDocument.Descendants(@"Серии").Descendants("Серия").Descendants("ДатаВыдачиСертиф").Any())
					line.CertificatesDate = position.XPathSelectElement("Серии/Серия/ДатаВыдачиСертиф").Value;

				if (xDocument.Descendants(@"Серии").Descendants("Серия").Descendants("ОрганСертиф").Any())
					line.CertificateAuthority = position.XPathSelectElement("Серии/Серия/ОрганСертиф").Value;

				if (hasProducerCost) {
					if (String.IsNullOrEmpty(position.XPathSelectElement("ЦенаИзг").Value)) line.ProducerCostWithoutNDS = null;
					else line.ProducerCostWithoutNDS = position.Get("ЦенаИзг");
				}
				else line.ProducerCostWithoutNDS = null;

				if (hasNds) {
					string nds = position.XPathSelectElement("СтавкаНДС").Value;
					nds = nds.Replace('%', ' ');
					nds = nds.Trim();
					line.Nds = (uint?)SafeConvert.ToDecimalInvariant(nds);
				}
				else {
					line.Nds = null;
				}

				line.SetSupplierCostByNds(line.Nds);
				
				if (position.XPathSelectElement("СуммаОптВклНДС") != null) {
					string sum = position.XPathSelectElement("СуммаОптВклНДС").Value;
					sum = sum.Replace('.', ',');
					line.Amount = SafeConvert.ToDecimal(sum);
				}

				if (position.XPathSelectElement("СуммаНДС") != null) {
					string sum = position.XPathSelectElement("СуммаНДС").Value;
					sum = sum.Replace('.', ',');
					line.NdsAmount = SafeConvert.ToDecimal(sum);
				}

				if (position.XPathSelectElement("ЦенаРозн") != null) {
					string sum = position.XPathSelectElement("ЦенаРозн").Value;
					sum = sum.Replace('.', ',');
					line.RetailCost = SafeConvert.ToDecimal(sum);
				}

				if (position.XPathSelectElement("ЕАН13") != null)
					line.EAN13 = position.XPathSelectElement("ЕАН13").Value;

				if (position.XPathSelectElement("ГТД") != null)
					line.BillOfEntryNumber = position.XPathSelectElement("ГТД").Value;

				if (xDocument.Descendants(@"Серии").Descendants("Серия").Descendants("СрокГодностиТовара").Any())
					line.Period = position.XPathSelectElement("Серии/Серия/СрокГодностиТовара").Value;

				if (position.XPathSelectElement("ЖНВЛС") != null && !String.IsNullOrEmpty(position.XPathSelectElement("ЖНВЛС").Value))
					line.VitallyImportant = SafeConvert.ToInt32(position.XPathSelectElement("ЖНВЛС").Value) == 1;
				else if (position.XPathSelectElement("ЖВНЛС") != null && !String.IsNullOrEmpty(position.XPathSelectElement("ЖВНЛС").Value))
					line.VitallyImportant = SafeConvert.ToInt32(position.XPathSelectElement("ЖВНЛС").Value) == 1;

				if (hasSerialNum)
					line.SerialNumber = position.XPathSelectElement("Серии/Серия/СерияТовара").Value;
				if (hasRegCost)
					line.RegistryCost = position.GetOptional("ЦенаГР");
				if (line.RegistryCost != null && line.RegistryCost > 0)
					line.VitallyImportant = true;
			}
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			var document = XDocument.Load(file);
			return document.XPathSelectElement("Документ/ЗаголовокДокумента/НомерДок") != null;
		}
	}
}