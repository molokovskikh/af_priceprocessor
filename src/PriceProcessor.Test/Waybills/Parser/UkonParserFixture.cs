using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class UkonParserFixture
	{
		[Test]
		public void Parse()
		{
			var parser = new UkonParser();
			var doc = new Document();
			parser.Parse(@"..\..\Data\Waybills\0004076.sst", doc);
			Assert.That(doc.DocumentLines.Count, Is.EqualTo(2));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("0000004076"));

			Assert.That(doc.DocumentLines[0].Product, Is.EqualTo("Солодкового корня сироп фл.100 г"));
			Assert.That(doc.DocumentLines[0].Certificates, Is.EqualTo("201109^РОСС RU.ФМ05.Д11132^01.12.11201109^74-2347154^25.11.09 ГУЗ ОЦСККЛ г. Челябинск"));
			Assert.That(doc.DocumentLines[0].Period, Is.EqualTo("01.12.11"));
			
			Assert.That(doc.DocumentLines[1].Product, Is.EqualTo("Эвкалипта настойка фл.25 мл"));
			Assert.That(doc.DocumentLines[1].Certificates, Is.EqualTo("151209^РОСС ФМ05.Д36360^01.12.14151209^74-2370989^18.01.10 ГУЗ ОЦСККЛ г. Челябинск"));
			Assert.That(doc.DocumentLines[1].Period, Is.EqualTo("01.12.14"));
		}
	}
}
