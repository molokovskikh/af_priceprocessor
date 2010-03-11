using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class ProtekParserFixture
	{
		[Test]
		public void Parse()
		{
			var parser = new ProtekParser();
			var doc = new Document();
			var document = parser.Parse(@"..\..\Data\Waybills\1008fo.pd", doc);
			Assert.That(document.DocumentLines.Count, Is.EqualTo(1));
		}
	}
}