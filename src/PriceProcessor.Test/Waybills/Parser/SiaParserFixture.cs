using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class SiaParserFixture
	{
		[Test]
		public void Parse()
		{
			var parser = new SiaParser();
			var doc = new Document();
			var document = parser.Parse(@"..\..\Data\Waybills\1016416.dbf", doc);
			Assert.That(document.DocumentLines.Count, Is.EqualTo(1));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("1016416"));
			Assert.That(document.DocumentLines[0].Product, Is.EqualTo("Пентамин 5% Р-р д/ин. 1мл Амп. Х10 Б"));
			Assert.That(document.DocumentLines[0].SupplierCost, Is.EqualTo(171.78));
			Assert.That(document.DocumentLines[0].SupplierCostWithoutNDS, Is.EqualTo(156.16));
			Assert.That(document.DocumentLines[0].Period, Is.EqualTo("01.06.2013"));
		}
	}
}
