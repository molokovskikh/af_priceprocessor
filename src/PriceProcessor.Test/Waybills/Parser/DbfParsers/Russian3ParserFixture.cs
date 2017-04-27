using System;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	public class Russian3ParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("00141601.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("141601"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("24/06/2013")));
			Assert.AreEqual("141601", doc.Invoice.InvoiceNumber);
			Assert.AreEqual(DateTime.Parse("24/06/2013"), doc.Invoice.InvoiceDate);
			Assert.AreEqual(5117.91, doc.Invoice.Amount);

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("025045002012"));
			Assert.That(line.Product, Is.EqualTo("Ингалятор компрес. Omron CompAir NE-C24-RU"));
			Assert.That(line.Producer, Is.EqualTo("Омрон (Япония)"));
			Assert.AreEqual("Япония", line.Country);
			Assert.AreEqual("РОСС JP.МЕ20.Д00600", line.Certificates);
			Assert.AreEqual(43861657, line.OrderId);
			Assert.AreEqual("10115040/100413/0001437", line.BillOfEntryNumber);
			Assert.AreEqual(4015672105652, line.EAN13);
			Assert.AreEqual(1834.25, line.ProducerCostWithoutNDS);
			Assert.AreEqual(1834.25, line.Amount);
			Assert.AreEqual(1834.25, line.SupplierCostWithoutNDS);
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Nds, Is.EqualTo(0));
		}
	}
}