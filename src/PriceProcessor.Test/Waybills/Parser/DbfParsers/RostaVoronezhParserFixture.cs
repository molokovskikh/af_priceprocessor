using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class RostaVoronezhParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("9978618.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("9978618"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("27.01.2011"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("131"));
			Assert.That(line.Product, Is.EqualTo("АЛКА-ЗЕЛЬТЦЕР ТБ ШИП №10 ЛИМ"));
			Assert.That(line.SerialNumber, Is.EqualTo("BTA9L16"));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(108.31));
			Assert.That(line.Certificates, Is.EqualTo("РОСС DE ФМ11 Д68638 Д"));
			Assert.That(line.Country, Is.EqualTo("ГЕРМАНИЯ"));
			Assert.That(line.Period, Is.EqualTo("01.05.2013"));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(124.41));
			Assert.That(line.Producer, Is.EqualTo("BAYER BITTERFELD GMBH"));
			Assert.That(line.SupplierCost, Is.EqualTo(119.14));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.VitallyImportant, !Is.True);
		}
	}
}