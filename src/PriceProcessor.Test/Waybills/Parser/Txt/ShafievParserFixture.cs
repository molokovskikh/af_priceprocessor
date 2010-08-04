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
	}
}