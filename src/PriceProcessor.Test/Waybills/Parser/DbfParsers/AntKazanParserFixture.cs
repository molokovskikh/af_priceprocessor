using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class AntKazanParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("П_889.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("п  889"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("20.09.2010"));
			var line = doc.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Таб.БАД\"ВИНИБИС С\"-120шт."));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Certificates, Is.EqualTo(null));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Producer, Is.EqualTo("ООО АНТ"));
			Assert.That(line.Period, Is.EqualTo(null));
			Assert.That(line.SerialNumber, Is.EqualTo(null));
			Assert.That(line.SupplierCost, Is.EqualTo(145));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(145));
		}

		[Test]
		public void KazmedServiceParse()
		{
			var doc = WaybillParser.Parse("001150.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("КМ00001150"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("27.06.2011"));
			Assert.That(doc.Lines.Count, Is.EqualTo(10));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("ЯЯЗ12107"));
			Assert.That(line.Product, Is.EqualTo("Бинт нестерильный индивидуальня упаковка 5х10  Ахт"));
			Assert.That(line.Unit, Is.EqualTo("шт"));
			Assert.That(line.Quantity, Is.EqualTo(30));
			Assert.That(line.SupplierCost, Is.EqualTo(5.20));
			Assert.That(line.Nds, Is.EqualTo(0));
			Assert.That(line.NdsAmount, Is.EqualTo(0.4727));
			Assert.That(line.Amount, Is.EqualTo(156.0000));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.АЯ56.В43157"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Producer, Is.EqualTo("ООО ПКФ\"Ахтамар\""));
			Assert.That(line.Period, Is.EqualTo(null));
			Assert.That(line.SerialNumber, Is.EqualTo(null));
		}
	}
}