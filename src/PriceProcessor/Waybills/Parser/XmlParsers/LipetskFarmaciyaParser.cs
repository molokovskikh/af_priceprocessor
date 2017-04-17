using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.XmlParsers
{
	public class LipetskFarmaciyaParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var xdocument = XDocument.Load(file);
			document.ProviderDocumentId = xdocument.XPathSelectElement("Документы/Документ").Attribute("НомерДокумента")?.Value;
			document.DocumentDate = NullableConvert.ToDateTime(xdocument.XPathSelectElement("Документы/Документ").Attribute("ДатаДокумента")?.Value);
			foreach (var element in xdocument.XPathSelectElements("Документы/Документ/СтрокаДокумента"))
			{
				var line = document.NewLine();
				line.Product = (string) element.Attribute("НоменклатураНаименованиеПолное");
				line.Producer = (string) element.Attribute("Производитель");
				line.Country = (string) element.Attribute("Страна");

				line.Period = (string) element.Attribute("СрокГодностиДата");
				line.RegistryCost = NullableConvert.ToDecimal(element.Attribute("ЦенаЗарегистрированная")?.Value);
				line.Certificates = (string) element.Attribute("Сертификат");
				line.CertificateAuthority = (string) element.Attribute("СертификатОрган");
				line.CertificatesDate = (string)element.Attribute("СертификатДатаНачала");
				line.CertificatesEndDate = NullableConvert.ToDateTime(element.Attribute("СертификатДатаОкончания")?.Value);
				line.EAN13 = NullableConvert.ToUInt64((string)element.Attribute("Штрихкод"));

				line.SupplierCostWithoutNDS = decimal.Parse((string)element.Attribute("ЦенаЗакупкиБезНДС"));
				line.ProducerCostWithoutNDS = decimal.Parse((string)element.Attribute("ЦенаПроизводителяБезНДС"));
				line.RetailCost = NullableConvert.ToDecimal(element.Attribute("ЦенаРозничная")?.Value);

				line.SerialNumber = (string)element.Attribute("Серия");
				line.Quantity = (uint?) element.Attribute("Количество");
				line.VitallyImportant = element.Attribute("ЖНВП")?.Value == "Да";
				line.Nds = NullableConvert.ToUInt32(element.Attribute("СтавкаНДС")?.Value.Replace("%", ""));
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