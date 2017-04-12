using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class UgFarm13769ParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("24532-0_0934348.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Рн-0934348"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("12.12.2012"));
			Assert.That(doc.Lines.Count, Is.EqualTo(13));
			var invoce = doc.Invoice;
			Assert.That(invoce.InvoiceNumber, Is.EqualTo("Рн-0934348"));
			Assert.That(invoce.InvoiceDate.Value.ToShortDateString(), Is.EqualTo("12.12.2012"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("612687"));
			Assert.That(line.Product, Is.EqualTo("Антигриппин д/взр таб №10 раств шип"));
			Assert.That(line.Quantity, Is.EqualTo(4));
			Assert.That(line.RegistryCost, Is.EqualTo(null));
			Assert.That(line.RegistryDate, Is.EqualTo(null));
			Assert.That(line.SupplierCost, Is.EqualTo(134.86));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(122.60));
			Assert.That(line.ProducerCost, Is.EqualTo(131.55));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(119.59));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SerialNumber, Is.EqualTo("01140212"));
			Assert.That(line.Period, Is.EqualTo("01.02.2015"));
			Assert.That(line.DateOfManufacture, Is.EqualTo(null));
			Assert.That(line.NdsAmount, Is.EqualTo(49.04));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.Producer, Is.EqualTo("Натур Продукт Европа Б.В. - Нидерланды"));
			Assert.That(line.Period, Is.EqualTo("01.02.2015"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10404054/310712/0006626/1"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ05.Д09153"));
			Assert.That(line.CertificatesDate, Is.EqualTo("01.02.2015"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ОЦС г.Екатеринбург"));
			Assert.That(line.EAN13, Is.EqualTo(8717627103237));
			Assert.That(line.Amount, Is.EqualTo(539.44));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(2.52));
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
		}
	}
}
