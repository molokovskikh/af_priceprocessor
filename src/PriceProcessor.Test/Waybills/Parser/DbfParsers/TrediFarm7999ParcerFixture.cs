using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class TrediFarm7999ParcerFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("00008962.dbf");
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("06.02.2013"));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("РНЧ-000000008962"));
			var invoice = doc.Invoice;
			Assert.That(invoice.RecipientAddress, Is.EqualTo("г. Волжск, ул.Ленина, 55"));
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("0000008699"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("00027888"));
			Assert.That(line.Product, Is.EqualTo("Бисакодил таб 5мг N30"));
			Assert.That(line.Quantity, Is.EqualTo(5));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ05.Д67870"));
			Assert.That(line.CertificatesDate, Is.EqualTo("01.06.2015"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ОЦС Екатеринбур"));
			Assert.That(line.Country, Is.EqualTo("РОССИЯ"));
			Assert.That(line.Producer, Is.EqualTo("Озон ООО"));
			Assert.That(line.Period, Is.EqualTo("01.06.2015"));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(3.59));
			Assert.That(line.Amount, Is.EqualTo(39.5));
			Assert.That(line.SupplierCost, Is.EqualTo(7.9));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(7.18));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(6.0608));
			Assert.That(line.ProducerCost, Is.EqualTo(6.67));
			Assert.That(line.RegistryCost, Is.EqualTo(15.88));
			Assert.That(line.SerialNumber, Is.EqualTo("050512"));
			Assert.That(line.VitallyImportant, Is.EqualTo(true));
		}
	}
}
