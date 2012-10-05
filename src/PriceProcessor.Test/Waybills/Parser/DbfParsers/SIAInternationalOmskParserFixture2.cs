using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class SIAInternationalOmskParserFixture2
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("15673435_СИА Интернейшнл-Омск(Р-127497).dbf");
			var line = doc.Lines[0];

			Assert.That(line.Code, Is.EqualTo("10876"));
			Assert.That(line.Product, Is.EqualTo("Афлубин Капли гомеопатические 50мл Фл.-кап. Б М"));
			Assert.That(line.Producer, Is.EqualTo("Рихард Биттнер АГ"));
			Assert.That(line.Country, Is.EqualTo("АВСТРИЯ"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(220.9));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(220.9));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierCost, Is.EqualTo(242.99));
			Assert.That(line.Amount, Is.EqualTo(242.99));
			Assert.That(line.NdsAmount, Is.EqualTo(22.09));
			Assert.That(line.SerialNumber, Is.EqualTo("7351389"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС AT.ФМ09.Д26367 (ООО \"ИФБ\")"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130110/171111/0008326/2"));
			Assert.That(line.Period, Is.EqualTo("01.10.2015"));
		}
	}
}
