using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class BssSpbParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("3902670_БСС(99900).DBF"); 
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("99900.00")); // ранее вместо точки была запятая(99900,00) и тест не проходил
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("03/06/2010")));
			Assert.That(doc.Lines.Count, Is.EqualTo(2));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("08283"));
			Assert.That(line.Product, Is.EqualTo("Реланиум р-р для в/в и в/м введ. 0,01/2мл №10"));
			Assert.That(line.Producer, Is.EqualTo("Польфа Варшавский фармацевтический з-д"));
			Assert.That(line.Country, Is.EqualTo("ПОЛЬША"));
			Assert.That(line.Quantity, Is.EqualTo(50.00));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.11.2014"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС PL.ФМ08.Д42224"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(125.27));
			Assert.That(line.SupplierCost, Is.EqualTo(137.80));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(116.48));
			Assert.That(line.SerialNumber, Is.EqualTo("03ЕХ1109"));
			Assert.That(line.RegistryCost, Is.EqualTo(141.88));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(7.55));
			Assert.That(line.VitallyImportant, Is.True);
			Assert.AreEqual("ФМ08 ОЦКК Москва", line.CertificateAuthority);
			Assert.AreEqual("28.12.2009", line.CertificatesDate);
		}

        [Test]
        public void Parse2()
        {
            var doc = WaybillParser.Parse("БСС-16093_195769.dbf");
            var line = doc.Lines[0];
            Assert.That(line.CertificatesEndDate, Is.EqualTo(DateTime.Parse("01/01/2018")));
        }

		/*
		 * Тест для http://redmine.analit.net/issues/55953
		 */
		[Test]
		public void Parse3()
		{
			var doc = WaybillParser.Parse("41554_115954.DBF");
			Assert.That(doc.Parser, Is.EqualTo("BssSpbParser"));
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
