using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class Medicine12795ParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("A0000128.DBF");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Рн-Г0000128"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("08.01.2013"));
			var invoce = doc.Invoice;
			Assert.That(invoce.AmountWithoutNDS, Is.EqualTo(8218.77));
			Assert.That(invoce.NDSAmount10, Is.EqualTo(670.93));
			Assert.That(invoce.NDSAmount18, Is.EqualTo(127.9));
			Assert.That(invoce.AmountWithoutNDS10, Is.EqualTo(6709.33));
			Assert.That(invoce.AmountWithoutNDS18, Is.EqualTo(710.57));
			Assert.That(invoce.AmountWithoutNDS0, Is.EqualTo(0));
			Assert.That(invoce.RecipientId, Is.EqualTo(301));
			Assert.That(invoce.InvoiceNumber, Is.EqualTo(null));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("128734"));
			Assert.That(line.Product, Is.EqualTo("Адельфан- эзидрекс ( табл. № 30) Sandoz Индия"));
			Assert.That(line.Producer, Is.EqualTo("Sandoz"));
			Assert.That(line.Country, Is.EqualTo("Индия"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(104.55));
			Assert.That(line.SupplierCost, Is.EqualTo(115.01));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.ProducerCost, Is.EqualTo(160));
			//Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(136.94));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("31.10.2015"));
			Assert.That(line.Certificates, Is.EqualTo("РОССINФМ11Д14785"));
			Assert.That(line.CertificatesDate, Is.EqualTo("31.10.2015"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ООО \"Формат качества\" РОСС RU 0001.11ФМ11"));
			Assert.That(line.RegistryCost, Is.EqualTo(0.00));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.SerialNumber, Is.EqualTo("ВZ3056"));
			Assert.That(line.EAN13, Is.EqualTo("0"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("16139"));
		}
	}
}
