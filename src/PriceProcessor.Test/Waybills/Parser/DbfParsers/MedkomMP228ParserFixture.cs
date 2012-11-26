using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class MedkomMP228ParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("NKL_00011016.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("ВЧЛ-011016"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("23.11.2012")));

			var l = doc.Lines[0];
			Assert.That(l.Product, Is.EqualTo("CONTEX №12 CLASSIC презервативы"));
			Assert.That(l.Producer, Is.EqualTo("AVK POLYPHARM Франция/LRC Products LTD В"));
			Assert.That(l.Quantity, Is.EqualTo(3));
			Assert.That(l.SupplierCostWithoutNDS, Is.EqualTo(172.73));
			Assert.That(l.Nds, Is.EqualTo(10));
			Assert.That(l.NdsAmount, Is.EqualTo(51.82));
			Assert.That(l.Code, Is.EqualTo("М-00001614"));
			Assert.That(l.SerialNumber, Is.EqualTo("1207012022"));
			Assert.That(l.Certificates, Is.EqualTo("РОСС FR.АЯ02.B40252"));
			Assert.That(l.CertificatesDate, Is.EqualTo("12.10.2010"));
			Assert.That(l.CertificateAuthority, Is.EqualTo("РОСС RU.0001.11АЯ02"));
			Assert.That(l.Period, Is.EqualTo("30.06.2017"));
			Assert.That(l.OrderId, Is.EqualTo(0));
			Assert.That(l.BillOfEntryNumber, Is.EqualTo("10129020/210812/0000708/1"));
			Assert.That(l.EAN13, Is.EqualTo("5060040302552"));
			Assert.That(l.ProducerCostWithoutNDS, Is.EqualTo(172.73));
			Assert.That(l.Country, Is.EqualTo("СОЕДИНЕННОЕ КОР"));
			Assert.That(l.VitallyImportant, Is.EqualTo(false));
			Assert.That(l.RegistryCost, Is.EqualTo(0));
			Assert.That(l.RegistryDate, Is.Null);
			Assert.That(l.Amount, Is.EqualTo(570));
			Assert.That(l.NdsAmount, Is.EqualTo(51.82));
			Assert.That(l.CountryCode, Is.EqualTo("410"));

			var invoice = doc.Invoice;
			Assert.That(invoice.RecipientId, Is.EqualTo(65));
			Assert.That(invoice.Amount, Is.EqualTo(2075));
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("011016/040"));
			Assert.That(invoice.InvoiceDate, Is.EqualTo(DateTime.Parse("23.11.2012")));
		}
	}
}
