using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class SiaChernozemie21ParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("Р-1600928.dbf");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Р-1600928"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("07.11.2012")));

			var line = doc.Lines[0];
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.RegistryCost, Is.EqualTo(111.87));
			Assert.That(line.Certificates, Is.EqualTo("РОСС FR.ФМ11.Д48505|15-OCT-2012|11-APR-2017|ROCC.RU.0001.11ФМ11 ООО \"Формат Качества\" г.Москва|Рош-Москва ЗАО                                                                  |||"));
			Assert.That(line.CertificatesDate, Is.Null);
			Assert.That(line.Code, Is.EqualTo("1805"));
			Assert.That(line.Product, Is.EqualTo("Бактрим 240мг/5мл 100мл Сусп. д/пр.внутрь Фл. Б М (R)"));
			Assert.That(line.Producer, Is.EqualTo("Ф.Хоффманн-Ля Рош Лтд/Сенекси С.а.С."));
			Assert.That(line.Country, Is.EqualTo("ФРАНЦИЯ"));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(111.84));
			Assert.That(line.NdsAmount, Is.EqualTo(13.06));
			Assert.That(line.SupplierCost, Is.EqualTo(143.64));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(130.58));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(16.7561));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Period, Is.EqualTo("11.04.2017"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10113100/091012/0037078/1"));
			Assert.That(line.SerialNumber, Is.EqualTo("F0864F71"));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Amount, Is.EqualTo(143.64));
		}
	}
}
