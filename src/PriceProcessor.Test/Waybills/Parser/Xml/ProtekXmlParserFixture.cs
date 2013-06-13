using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;
using System;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class ProtekXmlParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\8041496-001.xml");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("8041496-001"));
			Assert.That(doc.Lines[0].Code, Is.EqualTo("22761"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("АЛФАВИТ ТАБ. №210"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Аквион ЗАО"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Российская Феде"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(3));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(268.33));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(227.4));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(18));
			Assert.That(doc.Lines[0].VitallyImportant, Is.False);
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(202.13));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("535353"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("2010-02-05")));
		}

		[Test]
		public void Parse_no_country()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\206075848_1.xml");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("206075848/1"));
			Assert.That(doc.Lines[0].Code, Is.EqualTo("13056"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Адвантан крем д/нар. прим. 0,1% туба 15г №1"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Intendis Manufacturing S.p.a."));
			Assert.That(doc.Lines[0].Country, Is.Null);
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(3));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(326.26));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(296.60));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].VitallyImportant, Is.True);
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(281.54));
			Assert.That(doc.Lines[0].SerialNumber, Is.Empty);
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("2013-01-23")));
		}
	}
}