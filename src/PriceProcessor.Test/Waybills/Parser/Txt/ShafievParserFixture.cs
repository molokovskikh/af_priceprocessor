using System;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class ShafievParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("Shafiev.txt");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("0000008787"));
			Assert.That(doc.DocumentDate, Is.EqualTo(new DateTime(2008, 11, 13)));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("00007240"));
			Assert.That(line.Product, Is.EqualTo("Мягкая игрушка \"Бычок\""));
			Assert.That(line.Producer, Is.EqualTo("ООО компания Мир Детства"));
			Assert.That(line.Country, Is.EqualTo("Китай"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.ProducerCost, Is.EqualTo(142.93));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(144.37));
			Assert.That(line.Nds, Is.EqualTo(10));
		}

		[Test]
		public void Parse_waybill_from_moron()
		{
			var doc = WaybillParser.Parse("09180759.txt");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("9180759"));
			Assert.That(doc.DocumentDate, Is.EqualTo(new DateTime(2010, 07, 27)));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("6797"));
			Assert.That(line.Product, Is.EqualTo("Алфлутоп амп. 10мг/1мл №10"));
			Assert.That(line.Producer, Is.EqualTo("Biotehnos"));
			Assert.That(line.Country, Is.EqualTo("Румыния"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.ProducerCost, Is.EqualTo(1102.59));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(888.7));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SerialNumber, Is.EqualTo("3491009"));
			Assert.That(line.Period, Is.EqualTo("01.09.12"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС.RO.ФМ08.Д15964"));
		}
	}
}