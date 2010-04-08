using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;
using Castle.ActiveRecord;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class Moron_338_SpecialParserFixture
	{
		[Test]
		public void Parse()
		{
			DocumentLog documentLog = null;
			using (new SessionScope()) {
				var supplier = Supplier.Find(338);
				documentLog = new DocumentLog { Supplier = supplier, };
				documentLog.CreateAndFlush();
			}
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\3668585_5_00475628.dbf", documentLog) is Moron_338_SpecialParser);

			var document = WaybillParser.Parse(@"..\..\Data\Waybills\3668585_5_00475628.dbf", documentLog);

			Assert.That(document.ProviderDocumentId, Is.EqualTo("475628"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("07/04/2010")));
			Assert.That(document.Lines[0].Code, Is.EqualTo("2057,00"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Атенолол таб. 50мг №30"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Дания"));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(29.02));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(30.12));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(33.13));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(1.10));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(5));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(10.00));
		}
	}
}
