using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class FarmSet_VoronezhFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("PY362019.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("ФК000362019"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("25.10.2010"));
			var line = doc.Lines[0];
            //Assert.That(.S, Is.EqualTo("815575"));
			Assert.That(line.Code, Is.EqualTo("7045"));
			Assert.That(line.Product, Is.EqualTo("Апилак 0,01г №10 таб."));
			Assert.That(line.Producer, Is.EqualTo("Вифитех"));
			Assert.That(line.Country, Is.EqualTo("РОССИЯ"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(11.19));
			Assert.That(line.SupplierCost, Is.EqualTo(12.31));
			Assert.That(line.SerialNumber, Is.EqualTo("060410"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ08.Д98276"));
			Assert.That(line.Period, Is.EqualTo("01.05.2013"));
            Assert.That(line.ProducerCost, Is.EqualTo(10.4));
            Assert.That(line.RegistryCost, Is.EqualTo(0));
            Assert.That(line.VitallyImportant, !Is.True);
		}
	}
}

