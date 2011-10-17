using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;
using System;

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
	}
}
