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
		}
	}
}
