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
			Assert.That(document.Lines.Count, Is.EqualTo(1));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("1016416"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Пентамин 5% Р-р д/ин. 1мл Амп. Х10 Б"));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(171.78));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(156.16));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.06.2013"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ08.Д95450"));
		}

		[Test]
		public void Parse_with_vitally_important()
		{
			var parser = new SiaParser();
			var doc = new Document();
			parser.Parse(@"..\..\Data\Waybills\8916.dbf", doc);
			Assert.That(doc.Lines.Count, Is.EqualTo(7));
			Assert.That(doc.Lines[4].VitallyImportant, Is.True);
			Assert.That(doc.Lines[5].VitallyImportant, Is.False);
		}
	}
}
