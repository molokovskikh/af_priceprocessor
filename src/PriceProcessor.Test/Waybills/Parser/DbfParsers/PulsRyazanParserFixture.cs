using System;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class PulsRyazanParserFixture
	{
		[Test]
		public void CertificateTest()
		{
			var doc = WaybillParser.Parse("00184493.dbf");
			var line = doc.Lines[0];
			Assert.That(line.CertificateFilename, Is.EqualTo("108365-010213-o-1.jpg"));
		}

		[Test]
		public void Parse()
		{
			var now = DateTime.Now;
			var doc = WaybillParser.Parse("0020790.dbf");
			Assert.That(doc.ProviderDocumentId, !Is.Empty);
			Assert.That(doc.DocumentDate, Is.GreaterThanOrEqualTo(now));
			Assert.That(doc.Lines.Count, Is.EqualTo(15));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("174"));
			Assert.That(line.Product, Is.EqualTo("Алмагель А сусп. д/пр.внутрь фл. 170 мл. (мерн. ложка) х1"));
			Assert.That(line.Producer, Is.EqualTo("Balkanpharma Troya AD"));
			Assert.That(line.Country, Is.EqualTo("Болгария"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.12.2011"));
			Assert.That(line.Certificate, Is.EqualTo("РОСС BG.ФМ09.Д02834"));
			Assert.That(line.SupplierCost, Is.EqualTo(89.06));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(80.96));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(87.66));
			Assert.That(line.SerialNumber, Is.EqualTo("111209"));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(doc.Lines[4].VitallyImportant, Is.True);
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(-7.64));
		}
	}
}