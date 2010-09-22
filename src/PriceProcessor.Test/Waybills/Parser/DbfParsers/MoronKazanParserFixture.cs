using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class MoronKazanParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("476209.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("476209"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("29.06.2010"));
			
			Assert.That(doc.Lines.Count, Is.EqualTo(26));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("58115"));
			Assert.That(line.Product, Is.EqualTo("Lacalut white з/п 50мл"));
			Assert.That(line.Producer, Is.EqualTo("Dr. Theiss Naturwaren"));
			Assert.That(line.Country, Is.EqualTo("Германия"));
			Assert.That(line.Quantity, Is.EqualTo(4));
			Assert.That(doc.Lines[2].RegistryCost, Is.EqualTo(629.26));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(95.50));
			Assert.That(line.SupplierCost, Is.EqualTo(112.69));
			Assert.That(line.SerialNumber, Is.EqualTo("1003109"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС.DE.ПК04.В00065"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(4.46));
			Assert.That(line.ProducerCost, Is.EqualTo(91.42));
			Assert.That(doc.Lines[2].VitallyImportant, Is.EqualTo(true));
		}
	}
}