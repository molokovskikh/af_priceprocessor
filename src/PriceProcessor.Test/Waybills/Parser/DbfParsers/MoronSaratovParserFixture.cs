using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class MoronSaratovParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("00841624.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("841624,00"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("02.11.2010"));
			Assert.That(doc.Lines.Count, Is.EqualTo(14));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("47089,00"));
			Assert.That(line.Product, Is.EqualTo("Аскорутин таб. №50"));
			Assert.That(line.Producer, Is.EqualTo("Фармстандарт-Уфимский витам. з-д"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Quantity, Is.EqualTo(5));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierCost, Is.EqualTo(17.60));
			//Assert.That(line.ProducerCost, Is.EqualTo(16.50));
			Assert.That(line.SerialNumber, Is.EqualTo("410910"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС.RU.ФМ05.Д09976"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(5.5));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Period, Is.EqualTo("01.10.2013"));
		}
	}
}
