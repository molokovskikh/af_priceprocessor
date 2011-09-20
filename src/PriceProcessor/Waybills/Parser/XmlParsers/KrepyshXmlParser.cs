using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using Inforoom.PriceProcessor.Waybills.Models;

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

			document.ProviderDocumentId = xDocument.XPathSelectElement("Документ/ЗаголовокДокумента/НомерДок").Value;
			
			var docDate = xDocument.XPathSelectElement("Документ/ЗаголовокДокумента/ДатаДок").Value;
			if (!String.IsNullOrEmpty(docDate))
				document.DocumentDate = Convert.ToDateTime(docDate);
			foreach(var position in xDocument.XPathSelectElements(@"Документ/ТоварныеПозиции/ТоварнаяПозиция"))
			{
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
					line.SupplierPriceMarkup = Convert.ToDecimal(position.XPathSelectElement("НаценОпт").Value, CultureInfo.InvariantCulture);
				line.SupplierCostWithoutNDS = position.Get("ЦенаОпт");
				line.Certificates = position.XPathSelectElement("Серии/Серия/НомерСертиф").Value;

				if (xDocument.Descendants(@"Серии").Descendants("Серия").Descendants("ДатаВыдачиСертиф").Any())
					line.CertificatesDate = position.XPathSelectElement("Серии/Серия/ДатаВыдачиСертиф").Value;

				if (hasProducerCost)
				{
					if (String.IsNullOrEmpty(position.XPathSelectElement("ЦенаИзг").Value)) line.ProducerCost = null;
					else line.ProducerCost = position.Get("ЦенаИзг");
				}
				else line.ProducerCost = null;
				
				if(hasNds)
				{
					string nds = position.XPathSelectElement("СтавкаНДС").Value;
					nds = nds.Replace('%', ' ');
					nds = nds.Trim();                  
					line.Nds = (uint?)Convert.ToDecimal(nds, CultureInfo.InvariantCulture);
				}
				else
				{
					line.Nds = null;
				}
				
				line.SetSupplierCostByNds(line.Nds);

				if (xDocument.Descendants(@"Серии").Descendants("Серия").Descendants("СрокГодностиТовара").Any())
					line.Period = position.XPathSelectElement("Серии/Серия/СрокГодностиТовара").Value;

				if (position.XPathSelectElement("ЖНВЛС") != null && !String.IsNullOrEmpty(position.XPathSelectElement("ЖНВЛС").Value))
					line.VitallyImportant = Convert.ToInt32(position.XPathSelectElement("ЖНВЛС").Value) == 1;
				else if (position.XPathSelectElement("ЖВНЛС") != null && !String.IsNullOrEmpty(position.XPathSelectElement("ЖВНЛС").Value))
					line.VitallyImportant = Convert.ToInt32(position.XPathSelectElement("ЖВНЛС").Value) == 1;

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
