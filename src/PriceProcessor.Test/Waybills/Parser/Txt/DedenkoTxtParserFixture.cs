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
			Assert.IsTrue(DedenkoTxtParser.CheckFileFormat(@"..\..\Data\Waybills\ДДЕД0000396.txt"));
			var doc = WaybillParser.Parse("ДДЕД0000396.txt");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("ДДЕД0000396"));
			Assert.That(doc.DocumentDate, Is.EqualTo(new DateTime(2012, 04, 03)));
			var invoice = doc.Invoice;
			Assert.That(invoice.ShipperInfo, Is.EqualTo("ИП Деденко Виктория Владимировна"));
			Assert.That(invoice.ConsigneeInfo, Is.EqualTo("Петросян Л.В. ИП"));
			Assert.That(invoice.Amount, Is.EqualTo(603.28m));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("4607174430686"));
			Assert.That(line.Product, Is.EqualTo("\"NS\" крем д/лица дневной д/сухой кожи 50"));
			Assert.That(line.Producer, Is.EqualTo("Натура сиберика"));
			Assert.That(line.Country, Is.EqualTo("РОССИЯ"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(112.41));
			Assert.That(line.Period, Is.EqualTo("01.2015"));
		}
	}
}
