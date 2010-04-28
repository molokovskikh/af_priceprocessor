using System;
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
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("12.03.2010")));
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
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("2610709"));
		}

		[Test]
		public void Wired_vitally_important_flag()
		{
			parser.Parse(@"..\..\Data\Waybills\3633567_0_17202011.xml", document);
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("01.03.2010")));
		}

		[Test]
		public void Parse_with_null_registry_cost_value()
		{
			parser.Parse(@"..\..\Data\Waybills\3633111_2_3632591_1_1748104.xml", document);
			Assert.That(document.Lines.Count, Is.EqualTo(10));
			Assert.That(document.Lines[0].RegistryCost, Is.Null);
		}

		[Test]
		public void Parse_with_quantity_in_fractional_format()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\3677690_Катрен(6578711_6587855_044044).xml");

			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(3));
			Assert.That(doc.Lines[1].Quantity, Is.EqualTo(1));
		}

		[Test]
		public void Parse_OAC()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\3749510_ОАС(1798100).xml");
			
			Assert.That(doc.Lines.Count, Is.EqualTo(2));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("152660"));
		}

		[Test]
		public void Parse_data_file()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\3699498_Катрен_046729_.data");

			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("15.04.2010")));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("46729"));
			Assert.That(doc.Lines.Count, Is.EqualTo(13));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("ВИТРУМ ЙОД 100МКГ N120 ТАБЛ П/О"));
			Assert.That(doc.Lines[0].Code, Is.EqualTo("7688460"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Юнифарм Инк"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(72.29f));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.02.2012"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС US.ФМ08.Д22407"));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("VU011"));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(5.27));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(73.8707));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(83.71));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(76.1));
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
		}
	}
}
