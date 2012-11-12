using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class PandaUfaParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("W_425.dbf");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("АЛЛ00031587"));
			Assert.That(document.DocumentDate, Is.EqualTo(DateTime.Parse("07.11.2012")));

			var invoice = document.Invoice;
			Assert.That(invoice.SellerName, Is.EqualTo("ООО \"Панда\""));
			Assert.That(invoice.BuyerName, Is.EqualTo("Аптена аптека"));
			Assert.That(invoice.RecipientAddress, Is.EqualTo("Аптека № 2"));

			var line = document.Lines[0];
			Assert.That(line.Product, Is.EqualTo("\"Архыз\" 1,5 л газ ПЭТ Мин.вода"));
			Assert.That(line.Code, Is.Null);
			Assert.That(line.Producer, Is.Null);
			Assert.That(line.Country, Is.Null);
			Assert.That(line.Quantity, Is.EqualTo(12));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(25.42));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(0));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.BillOfEntryNumber, Is.Null);
			Assert.That(line.EAN13, Is.Null);
			Assert.That(line.SerialNumber, Is.Null);
			Assert.That(line.Certificates, Is.Null);
			Assert.That(line.CertificatesDate, Is.Null);
			Assert.That(line.CertificateAuthority, Is.Null);
			Assert.That(line.Period, Is.Null);
		}
	}
}
