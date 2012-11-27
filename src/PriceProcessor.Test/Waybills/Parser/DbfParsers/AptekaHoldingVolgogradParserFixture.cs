using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	class AptekaHoldingVolgogradParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("3518_1090425.dbf");

			Assert.That(document.ProviderDocumentId, Is.EqualTo("АХ1-1090425/0"));
			Assert.That(document.DocumentDate, Is.EqualTo(new DateTime(2012, 9, 25)));

			var invoice = document.Invoice;
			Assert.That(invoice.RecipientAddress, Is.EqualTo("N13 г.Астрахань, ул.Чалабяна/Ногина/Свердлова, д.19/7/98, Лит.Б"));
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("АХ1-1090425/0"));
			Assert.That(invoice.InvoiceDate, Is.EqualTo(new DateTime(2012, 9, 25)));
			Assert.That(invoice.SellerAddress, Is.Null);
			Assert.That(invoice.SellerINN, Is.Null);
			Assert.That(invoice.SellerKPP, Is.Null);
			Assert.That(invoice.SellerName, Is.Null);

			var line = document.Lines[0];
			Assert.That(line.SerialNumber, Is.EqualTo("21580411"));
			Assert.That(line.Period, Is.EqualTo("01.04.2014"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС NL.ФМ05.Д02656"));
			Assert.That(line.CertificatesDate, Is.Null);
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(80.5));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(80.7));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierCost, Is.EqualTo(88.77));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0.25));
			Assert.That(line.NdsAmount, Is.EqualTo(24.21));
			Assert.That(line.Amount, Is.EqualTo(266.31));
			Assert.That(line.CertificateAuthority, Is.Null);
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.EAN13, Is.Null);
			Assert.That(line.Producer, Is.EqualTo("Natur Produkt"));
			Assert.That(line.Product, Is.EqualTo("Анти-Ангин формула пастилки N24 Нидерланды"));
			Assert.That(line.Code, Is.EqualTo("15337"));
		}
	}
}
