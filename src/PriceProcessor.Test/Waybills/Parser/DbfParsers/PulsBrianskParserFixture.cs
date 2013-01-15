using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class PulsBrianskParserFixture
	{
		[Test]
		public void PulsBrianskParser()
		{
			var doc = WaybillParser.Parse("00004858.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("00004858"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("10.01.2013"));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("14251"));
			Assert.That(line.Product, Is.EqualTo("Амитриптилин табл. 25 мг х50"));
			Assert.That(line.SerialNumber, Is.EqualTo("130912"));
			Assert.That(line.Period, Is.EqualTo("01.10.2015"));
			Assert.That(line.SupplierCost, Is.EqualTo(13.19));
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ05.Д40352"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ООО \"ОЦС\" г. Екатеринбург"));
			Assert.That(line.CertificatesDate, Is.EqualTo("05.10.2012"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(4.72));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(43.01));
			Assert.That(line.Producer, Is.EqualTo("Озон"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(11.99));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(11.45));
			Assert.That(line.NdsAmount, Is.EqualTo(12m));
			Assert.That(line.Amount, Is.EqualTo(131.9));
			Assert.That(line.OrderId, Is.EqualTo(37468018));
			Assert.That(line.VitallyImportant, Is.EqualTo(true));
			Assert.That(doc.Invoice.RecipientId, Is.EqualTo(13505));
			Assert.That(doc.Invoice.RecipientAddress, Is.EqualTo("307340, Курская обл., Рыльский р-н, с. Ивановское, ул. Ананьева, д. 1б"));
		}
	}
}
