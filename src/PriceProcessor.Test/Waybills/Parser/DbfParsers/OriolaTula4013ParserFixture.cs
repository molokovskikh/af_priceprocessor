using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class OriolaTula4013ParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("739678.dbf");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("739678"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("19.10.2012")));

			var invoice = doc.Invoice;
			Assert.That(invoice.NDSAmount10, Is.EqualTo(816.41));
			Assert.That(invoice.NDSAmount18, Is.EqualTo(33.03));
			Assert.That(invoice.AmountWithoutNDS10, Is.EqualTo(8164.1));
			Assert.That(invoice.AmountWithoutNDS18, Is.EqualTo(183.5));
			Assert.That(invoice.AmountWithoutNDS0, Is.EqualTo(0));
			Assert.That(invoice.RecipientId, Is.EqualTo(6288));
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("739678"));
			Assert.That(invoice.InvoiceDate, Is.EqualTo(DateTime.Parse("19.10.2012")));
			Assert.That(invoice.Amount, Is.EqualTo(9197.04));

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("76947"));
			Assert.That(line.EAN13, Is.EqualTo(7332343001356));
			Assert.That(line.SupplierCost, Is.EqualTo(226.6));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(206));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.SerialNumber, Is.EqualTo("12031727"));
			Assert.That(line.Period, Is.EqualTo("01.03.2015"));
			Assert.That(line.Product, Is.EqualTo("Аквалор Горло спр алоэ/ром.д/д,взр.125мл"));
			Assert.That(line.Country, Is.EqualTo("Швеция"));
			Assert.That(line.Producer, Is.EqualTo("Aurena Laboratories"));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.RegistryDate, Is.Null);
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130070/060412/0007935/1"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС.SE.ИМ25.А03917"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ОРГАН ПО СЕРТИФИКАЦИИ \"ЭНЕРГИЯ ПЛЮС\""));
			Assert.That(line.CertificatesDate, Is.EqualTo("24.09.2010"));
			Assert.That(line.Amount, Is.EqualTo(226.6));
			Assert.That(line.OrderId, Is.EqualTo(1));
			Assert.That(line.NdsAmount, Is.EqualTo(20.6));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(206));
		}
	}
}
