using System;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class BssSpbWithEan13ParserFixture
	{
		/*
		 * Тест для http://redmine.analit.net/issues/55953
		 */
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("41554_115954.DBF");
			Assert.That(doc.Parser, Is.EqualTo("BssSpbWithEan13Parser"));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("115954"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("18.10.2016")));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("51528"));
			Assert.That(line.Product, Is.EqualTo("Дигоксин таб. 0,25мг №50 Гедеон"));
			Assert.That(line.Producer, Is.EqualTo("Гедеон Рихтер ОАО/Гедеон Рихтер-Рус ЗАО"));
			Assert.That(line.Country, Is.EqualTo("РОССИЯ"));
			Assert.That(line.Quantity, Is.EqualTo(1.00));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.04.2019"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФВ14.Д27505"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(42.40));
			Assert.That(line.SupplierCost, Is.EqualTo(46.64));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(36.59));
			Assert.That(line.SerialNumber, Is.EqualTo("400816"));
			Assert.That(line.RegistryCost, Is.EqualTo(36.59));
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.CertificateAuthority, Is.EqualTo("ФВ14 ЗАО \"Техкачество\""));
			Assert.That(line.CertificatesDate, Is.EqualTo("30.08.2016"));
			Assert.That(line.EAN13, Is.EqualTo("4605469000743"));
		}
	}
}
