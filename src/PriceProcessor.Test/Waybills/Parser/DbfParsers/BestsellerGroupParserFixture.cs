using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class BestsellerGroupParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("R_3470.DBF");
			Assert.That(document.Lines.Count, Is.EqualTo(46));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("3470"));
			Assert.That(document.DocumentDate, Is.EqualTo(new DateTime(2012, 9, 11)));
			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("Р12941"));
			Assert.That(line.Product, Is.EqualTo("1010а Бутылочка пл. 240мл сил/соск быстр. поток/6+ /аква/12"));
			Assert.That(line.SerialNumber, Is.Null);
			Assert.That(line.Period, Is.Null);
			Assert.That(line.SupplierCost, Is.EqualTo(81.81));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Certificates, Is.Null);
			Assert.That(line.CertificateAuthority, Is.Null);
			Assert.That(line.CertificatesDate, Is.Null);
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Producer, Is.Null);
			Assert.That(line.Country, Is.EqualTo("Китай"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(69.3305));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(0));
			Assert.That(line.NdsAmount, Is.EqualTo(12.4795));
			Assert.That(line.Amount, Is.EqualTo(81.81));
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
			Assert.That(document.Invoice.AmountWithoutNDS, Is.EqualTo(69.3305));
		}
	}
}
