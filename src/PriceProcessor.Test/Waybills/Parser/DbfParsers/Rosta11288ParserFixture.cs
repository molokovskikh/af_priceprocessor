using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class Rosta11288ParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("i_11988703.dbf");

			Assert.That(document.ProviderDocumentId, Is.EqualTo("11988703"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("30.11.2012")));

			Assert.That(document.Invoice.InvoiceNumber, Is.EqualTo("11988703"));
			Assert.That(document.Invoice.InvoiceDate, Is.EqualTo(Convert.ToDateTime("30.11.2012")));
			Assert.That(document.Invoice.AmountWithoutNDS, Is.EqualTo(2570.84));
			Assert.That(document.Invoice.RecipientId, Is.EqualTo(3406011));
			Assert.That(document.Invoice.Amount, Is.EqualTo(2840.12));
			Assert.That(document.Invoice.NDSAmount, Is.EqualTo(269.28));

			Assert.That(document.Lines[0].Code, Is.EqualTo("000191"));
			Assert.That(document.Lines[0].EAN13, Is.EqualTo(4030096245128));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(88.12));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(95.58));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(105.14));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("M24613"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.05.2017"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("АМБРОБЕНЕ СИРОП 15МГ/5МЛ 100МЛ"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("ГЕРМАНИЯ"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Merckle - Германия"));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(document.Lines[0].VitallyImportant, Is.EqualTo(true));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(88.27));
			Assert.That(document.Lines[0].BillOfEntryNumber, Is.EqualTo("10130032/191012/0006938/1"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС DE ФМ08 Д23956 ДО 01.05.17 рег.№ 167840А"));
			Assert.That(document.Lines[0].CertificateAuthority, Is.EqualTo("ОЦС Хабаровский край"));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(9.56));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(105.14));
		}
	}
}
