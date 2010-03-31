using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class SiaXmlParserFixture
	{
		private SiaXmlParser parser;
		private Document document;

		[SetUp]
		public void Setup()
		{
			parser = new SiaXmlParser();
			document = new Document();
		}

		[Test]
		public void Parse()
		{
			parser.Parse(@"..\..\Data\Waybills\1039428.xml", document);
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Р-1039428"));
			Assert.That(document.Lines.Count, Is.EqualTo(5));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Вазилип 10мг Таб.П/плен.об  Х28"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("КРКА-РУС/KRKA d.d."));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(242.88));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(250.59));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(227.81));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.07.2012"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ01.Д50494"));
		}

		[Test]
		public void Wired_vitally_important_flag()
		{
			parser.Parse(@"..\..\Data\Waybills\3633567_0_17202011.xml", document);
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
		}

		[Test]
		public void Parse_with_null_registry_cost_value()
		{
			parser.Parse(@"..\..\Data\Waybills\3633111_2_3632591_1_1748104.xml", document);
			Assert.That(document.Lines.Count, Is.EqualTo(10));
			Assert.That(document.Lines[0].RegistryCost, Is.Null);
		}
	}
}
