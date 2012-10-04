using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class RostaOmskParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("15790421_Роста(129339).DBF");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("129339/3"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("28.09.2012")));

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("80852"));
			Assert.That(line.Product, Is.EqualTo("Новосепт апельсин паст х 24"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(48.01));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.NdsAmount, Is.EqualTo(25.93));
			Assert.That(line.Amount, Is.EqualTo(169.96));
			Assert.That(line.SerialNumber, Is.EqualTo("25031211"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС NL ФМ05 Д89796"));
			Assert.That(line.Producer, Is.EqualTo("Natur Produkt Europa B.V. - Нидерланды"));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(46.25));
			Assert.That(line.Country, Is.EqualTo("Нидерланды"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10404054/150612/0005121/1"));
			Assert.That(line.EAN13, Is.EqualTo("4601372002973"));

			Assert.That(doc.Invoice.ShipperInfo, Is.EqualTo("РОСТА"));
		}
	}
}
