using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class Medicine12795ParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("A0000008.DBF");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Рн-Г0000008"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("03.01.2013"));
			var invoce = doc.Invoice;
			Assert.That(invoce.AmountWithoutNDS, Is.EqualTo(1105.53));
			Assert.That(invoce.NDSAmount10, Is.EqualTo(98.93));
			Assert.That(invoce.NDSAmount18, Is.EqualTo(2.65));
			Assert.That(invoce.AmountWithoutNDS10, Is.EqualTo(989.25));
			Assert.That(invoce.AmountWithoutNDS18, Is.EqualTo(14.7));
			Assert.That(invoce.AmountWithoutNDS0, Is.EqualTo(0));
			Assert.That(invoce.RecipientId, Is.EqualTo(301));
			Assert.That(invoce.InvoiceNumber, Is.EqualTo(null));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("133074"));
			Assert.That(line.Product, Is.EqualTo("Бинт медицинский ст. ( 10м.х16см.пресс.) Лейко ООО Россия"));
			Assert.That(line.Producer, Is.EqualTo("Лейко ООО"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(12.45));
			Assert.That(line.SupplierCost, Is.EqualTo(13.7));
			Assert.That(line.Quantity, Is.EqualTo(20));
			Assert.That(line.ProducerCost, Is.EqualTo(12.45));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(8.3));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("14.08.2017"));
			Assert.That(line.Certificates, Is.EqualTo("РОССRUИМ09В02359"));
			Assert.That(line.CertificatesDate, Is.EqualTo("14.08.2017"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("Орган по сертиф.перевязочн,шовн.и полимерн.материалов"));
			Assert.That(line.RegistryCost, Is.EqualTo(0.00));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.SerialNumber, Is.EqualTo("140812"));
			Assert.That(line.EAN13, Is.EqualTo(0));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("         0"));
		}
	}
}
