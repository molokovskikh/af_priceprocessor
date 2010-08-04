using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class GenesisNNParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("GenesisNN.dbf");

			Assert.That(doc.Lines.Count, Is.EqualTo(10));
			var line = doc.Lines[1];
			Assert.That(line.Code, Is.EqualTo("5576697"));
			Assert.That(line.Product, Is.EqualTo("ÀÑÊÎĞÁÈÍÎÂÀß Ê-ÒÀ 0,05 N200 ÄĞÀÆÅ"));
			Assert.That(line.Producer, Is.EqualTo("ÌÀĞÁÈÎÔÀĞÌ ÎÀÎ"));
			Assert.That(line.Country, Is.EqualTo("ğîññèÿ"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.ProducerCost, Is.EqualTo(9.69));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(11));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(9.69));
			Assert.That(line.Period, Is.EqualTo("01.04.2012"));
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.SerialNumber, Is.EqualTo("790310"));
			Assert.That(line.Certificates, Is.EqualTo("ĞÎÑÑ RU.ÔÌ05.Ä87777"));
		}
	}
}