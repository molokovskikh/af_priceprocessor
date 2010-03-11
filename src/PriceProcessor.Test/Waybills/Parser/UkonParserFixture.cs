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
			Assert.That(doc.DocumentLines[0].Product, Is.EqualTo("Солодкового корня сироп фл.100 г"));
			Assert.That(doc.DocumentLines[1].Product, Is.EqualTo("Эвкалипта настойка фл.25 мл"));
		}
	}
}
