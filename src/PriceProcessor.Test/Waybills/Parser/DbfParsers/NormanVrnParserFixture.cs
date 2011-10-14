using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class NormanVrnParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("00376611.dbf");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("00376611"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("06.12.2010"));
			Assert.That(document.Lines.Count, Is.EqualTo(8));
			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("101565"));
			Assert.That(line.Product, Is.EqualTo("Дротаверин г/хл амп. 2%-2мл №10"));
			Assert.That(line.Producer, Is.EqualTo("Эллара (РОССИЯ)"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(15.95));
			Assert.That(line.SerialNumber, Is.EqualTo("030510"));
			Assert.That(line.Period, Is.EqualTo("01.06.2012"));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(40));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU ФМ08 Д31270"));
		}
	}
}