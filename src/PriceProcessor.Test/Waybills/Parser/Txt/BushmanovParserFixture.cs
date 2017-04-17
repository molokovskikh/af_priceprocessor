using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class BushmanovParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("Мечта-2 (Слободской, Советская 99) - 604-А.tdf");
			Assert.That(doc.Lines.Count, Is.EqualTo(60));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("604"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("010102-021"));
			Assert.That(line.EAN13, Is.EqualTo("4600697101576"));
			Assert.That(line.Product, Is.EqualTo("Мыло в спайке \"ДЕТСКОЕ\" [NEW Ромашка] {4 шт. по 100 г}"));
			Assert.That(line.Producer, Is.EqualTo("Невская Косметика"));
			Assert.That(line.Country, Is.EqualTo("РОССИЯ"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.SupplierCost, Is.EqualTo(59.51));
			Assert.That(line.Amount, Is.EqualTo(178.53));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.Certificates, Is.EqualTo("RU Д-RU.АЯ61.В.00074"));
			Assert.That(line.CertificatesDate, Is.EqualTo("09.09.11"));
			Assert.That(line.CertificatesEndDate, Is.EqualTo(Convert.ToDateTime("08.09.2016")));
			Assert.That(line.CertificateAuthority, Is.EqualTo("СПБ ГУ ЦККТРУ"));
		}
	}
}
