using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class SiaAstrahan12423ParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("P-856773.DBF");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Р-856773"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("17.11.2012")));

			var l = doc.Lines[3];
			Assert.That(l.Code, Is.EqualTo("1496"));
			Assert.That(l.Product, Is.EqualTo("Бифидумбактерин Сух.пор. д/пр внутрь и мест. прим. Пак. Х30 М (R)"));
			Assert.That(l.Producer, Is.EqualTo("Партнер Ао, РОССИЯ"));
			Assert.That(l.Country, Is.Null);
			Assert.That(l.ProducerCostWithoutNDS, Is.EqualTo(174.04));
			Assert.That(l.ProducerCost, Is.EqualTo(191.444));
			Assert.That(l.SupplierCostWithoutNDS, Is.EqualTo(28.8));
			Assert.That(l.SupplierCost, Is.EqualTo(31.68));
			Assert.That(l.RegistryCost, Is.EqualTo(174.04));
			Assert.That(l.SupplierPriceMarkup, Is.EqualTo(-83.4521));
			Assert.That(l.Amount, Is.EqualTo(31.68));
			Assert.That(l.NdsAmount, Is.EqualTo(2.88));
			Assert.That(l.Quantity, Is.EqualTo(1));
			Assert.That(l.Period, Is.EqualTo("01.12.2012"));
			Assert.That(l.Certificates, Is.EqualTo("РОСС RU.ФМ13.В04323"));
			Assert.That(l.CertificatesDate, Is.Null);
			Assert.That(l.SerialNumber, Is.EqualTo("273-21111"));
			Assert.That(l.BillOfEntryNumber, Is.Null);
			Assert.That(l.EAN13, Is.EqualTo("4600561020019"));
			Assert.That(l.Nds, Is.EqualTo(10));
			Assert.That(l.VitallyImportant, Is.True);
			Assert.That(doc.Lines[0].VitallyImportant, Is.False);
		}
	}
}
