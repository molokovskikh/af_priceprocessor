using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Parser.TxtParsers;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class DedenkoTxtParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(DedenkoTxtParser.CheckFileFormat(@"..\..\Data\Waybills\ДДЕД0000419_2.txt"));
			var doc = WaybillParser.Parse("ДДЕД0000419_2.txt");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("ДДЕД0000419"));
			Assert.That(doc.DocumentDate, Is.EqualTo(new DateTime(2012, 04, 10)));
			var invoice = doc.Invoice;
			Assert.That(invoice.ShipperInfo, Is.EqualTo("ИП Деденко Виктория Владимировна"));
			Assert.That(invoice.RecipientAddress, Is.EqualTo("Базарова Н.В. ИП"));
			Assert.That(invoice.Amount, Is.EqualTo(1988.06m));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("00000000134"));
			Assert.That(line.Product, Is.EqualTo("\"NS\" активная маска для лица 75 мл."));
			Assert.That(line.Producer, Is.EqualTo("Натура сиберика"));
			Assert.That(line.Country, Is.EqualTo("РОССИЯ"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(67.18));
			Assert.That(line.SupplierCost, Is.EqualTo(67.18));
			Assert.That(line.Nds, Is.EqualTo(0));
			Assert.That(line.Period, Is.EqualTo("01.2015"));
		}
	}
}