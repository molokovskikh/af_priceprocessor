using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	class KrepyshXmlParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\20101119_8055_250829.xml");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("8055"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("19.11.2010")));
			Assert.That(document.Lines.Count, Is.EqualTo(14));
			Assert.That(document.Lines[0].Code, Is.EqualTo("5443"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Прокладки \"Котекс\" Део ежедневные 1 пач/20шт"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo(" \"Yuhan-Kimberly Ltd\", Республика Корея"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(28.57));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(25.97));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС CN.АВ57.В05475"));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(null));
			Assert.That(document.Lines[0].Period, Is.EqualTo(null));
			Assert.That(document.Lines[0].CertificatesDate, Is.EqualTo("10.08.2009"));
		}

		[Test]
		public void ParseFix()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\125968.xml");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Рн-Иж0000125968"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("08.12.2010")));
			Assert.That(document.Lines.Count, Is.EqualTo(4));
			Assert.That(document.Lines[0].Code, Is.EqualTo(null));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Альфарона 50тысМЕ пор д/пр наз р-ра №1"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Фармаклон НПП ООО"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("РОССИЯ"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(106.91818));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(null));
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(100));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.11.2011"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС RU.ФМ08.Д96848 "));
			Assert.That(document.Lines[0].CertificatesDate, Is.EqualTo("03.12.2009"));
		}

        [Test]
        public void Parse_without_producer_cost_and_nds_with_symbol_percent()
        {
            var document = WaybillParser.Parse(@"..\..\Data\Waybills\8817928.xml");
            document = WaybillParser.Parse(@"..\..\Data\Waybills\8817930.xml");
            document = WaybillParser.Parse(@"..\..\Data\Waybills\8817942.xml");
        }
	}
}
