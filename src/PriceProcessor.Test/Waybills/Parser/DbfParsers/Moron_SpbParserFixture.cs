using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class Moron_SpbParserFixture
	{
		[Test]
		public void Parse()
		{
            var doc = WaybillParser.Parse("00815575.dbf");
            Assert.That(doc.ProviderDocumentId, Is.EqualTo("815575"));
			//Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("20.09.2010"));
			var line = doc.Lines[0];
            //Assert.That(.S, Is.EqualTo("815575"));
            Assert.That(line.Product, Is.EqualTo("Оциллококцинум гран. 1г №6доз"));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.ProducerCost, Is.EqualTo(168.43));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(155.20));
			Assert.That(line.SupplierCost, Is.EqualTo(170.72));
			Assert.That(line.RegistryCost, Is.EqualTo(0.00));
			Assert.That(line.Period, Is.EqualTo("01.12.2014"));
            Assert.That(line.SerialNumber, Is.EqualTo("09382"));
            Assert.That(line.Country, Is.EqualTo("Франция"));
            Assert.That(line.Producer, Is.EqualTo("Laboratoires Boiron"));
            Assert.That(line.Certificates, Is.EqualTo("РОСС.FR.ФМ08.Д37876"));
            Assert.That(line.Code, Is.EqualTo("17917"));
            Assert.That(line.VitallyImportant, !Is.True);
		}
	}
}