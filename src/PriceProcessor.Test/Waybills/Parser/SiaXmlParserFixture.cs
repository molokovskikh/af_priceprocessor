using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class SiaXmlParserFixture
	{
		[Test]
		public void Parse()
		{
			var parser = new SiaXmlParser();
			var document = new Document();
			parser.Parse(@"..\..\Data\Waybills\1039428.xml", document);
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Р-1039428"));
			Assert.That(document.Lines.Count, Is.EqualTo(5));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Вазилип 10мг Таб.П/плен.об  Х28"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("КРКА-РУС/KRKA d.d."));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(242.88));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(227.81));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(207.10));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.07.2012"));
		}
	}
}
