using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class Organika13449ParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("А423789.dbf");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("А-423789"));
			Assert.That(document.DocumentDate, Is.EqualTo(DateTime.Parse("31.10.2012")));
			Assert.That(document.Invoice.Amount, Is.EqualTo(5363.18));
			Assert.That(document.Invoice.RecipientName, Is.EqualTo("ИП Церенова Е.Н."));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("590607"));
			Assert.That(line.Product, Is.EqualTo("Амигренин (таб. 100 мг №2 )"));
			Assert.That(line.Producer, Is.EqualTo("Верофарм ЗАО"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(210.74));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(1.19));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(213.25));
			Assert.That(line.Amount, Is.EqualTo(469.14));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SerialNumber, Is.EqualTo("91011"));
			Assert.That(line.Certificates, Is.EqualTo("ФГУ ЦЭиКМП РОСДРАВНАДЗОР №РОСС RU.ФМ01.Д46222 до 01.11.13"));
			Assert.That(line.Period, Is.EqualTo("01.11.2013"));
			Assert.That(line.BillOfEntryNumber, Is.Null);
			Assert.That(line.EAN13, Is.EqualTo("4602930005214"));
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
		}
	}
}
