using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class Rosta11288ParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("11888316.dbf");

			Assert.That(document.ProviderDocumentId, Is.EqualTo("11888316"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("30.10.2012")));

			Assert.That(document.Invoice.InvoiceNumber, Is.EqualTo("11888316"));
			Assert.That(document.Invoice.InvoiceDate, Is.EqualTo(Convert.ToDateTime("30.10.2012")));
			Assert.That(document.Invoice.AmountWithoutNDS, Is.EqualTo(2221.72));
			Assert.That(document.Invoice.RecipientAddress, Is.EqualTo("3401029"));

			Assert.That(document.Lines[0].Code, Is.EqualTo("000245"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("АМОКСИКЛАВ ТБ 0,375 №15"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Lek"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("СЛОВЕНИЯ"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(173.1));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(190.41));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(2443.9));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(222.18));
			Assert.That(document.Lines[0].EAN13, Is.EqualTo("3838957699902"));
			Assert.That(document.Lines[0].BillOfEntryNumber, Is.EqualTo("10113100/130712/0024844/1"));
			Assert.That(document.Lines[0].VitallyImportant, Is.EqualTo(true));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("CM4600"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.05.2014"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС SI ФМ08 Д76974 ДО 01.05.14 рег.№ 146634А"));
			Assert.That(document.Lines[0].CertificateAuthority, Is.EqualTo("ОЦС Хабаровский край"));
			Assert.That(document.Lines[0].CertificatesDate, Is.EqualTo("20.07.2012"));
			Assert.That(document.Lines[0].OrderId, Is.EqualTo(11888316));
		}
	}
}
