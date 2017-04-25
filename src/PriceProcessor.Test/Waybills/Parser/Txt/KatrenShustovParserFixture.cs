using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class KatrenShustovParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse(@"244580-03.TXT");

			Assert.That(doc.Lines.Count, Is.EqualTo(17));
			Assert.That(doc.Invoice.InvoiceNumber, Is.EqualTo("244580-03"));
			Assert.That(doc.Invoice.InvoiceDate, Is.EqualTo(Convert.ToDateTime("24.04.2017")));
			Assert.That(doc.Invoice.DateOfPaymentDelay, Is.EqualTo("08.05.2017"));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("4237031"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("ВАТА ХИРУРГИЧЕСКАЯ СТЕРИЛЬНАЯ АМЕЛИЯ МАЛЫШ 100,0"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Гигровата-Санкт-Петербург,ЗАО"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("россия"));
			Assert.That(doc.Lines[0].VitallyImportant, Is.EqualTo(false));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("032022"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.03.2022"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС RU.АБ69.Д01261"));
			Assert.That(doc.Lines[0].CertificatesEndDate, Is.EqualTo(Convert.ToDateTime("28.01.2020")));
			Assert.That(doc.Lines[0].CertificateAuthority, Is.EqualTo("АБ69"));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(39.49M));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(doc.Lines[0].RegistryDate, Is.EqualTo(null));
			Assert.That(doc.Lines[0].EAN13, Is.EqualTo(4600665203721));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(35.90M));
			Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(394.90M));

			Assert.That(doc.Lines[1].RegistryDate, Is.EqualTo(Convert.ToDateTime("11.06.2015 0:00:00")));
		}
	}
}