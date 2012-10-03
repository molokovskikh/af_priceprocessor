using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class RafelKazanParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("01124813.dbf");

			Assert.That(document.ProviderDocumentId, Is.EqualTo("Рн-Kz0001124813"));
			Assert.That(document.DocumentDate, Is.EqualTo(new DateTime(2012, 9, 25)));

			var invoice = document.Invoice;
			Assert.That(invoice.RecipientAddress, Is.EqualTo("№13 г.Казань, ул.Чехова, д.4"));
			var line = document.Lines[0];
			Assert.That(line.SerialNumber, Is.EqualTo("010211"));
			Assert.That(line.Period, Is.EqualTo("01.02.2014"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ05.Д01957"));
			Assert.That(line.CertificatesDate, Is.Null);
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(8.6));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(11.22));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierCost, Is.EqualTo(12.34));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.Producer, Is.EqualTo("ФитоФарм ПКФ ООО"));
			Assert.That(line.Product, Is.EqualTo("Багульника болотного побеги пачка 35г №1"));
			Assert.That(line.EAN13, Is.Null);
		}
	}
}
