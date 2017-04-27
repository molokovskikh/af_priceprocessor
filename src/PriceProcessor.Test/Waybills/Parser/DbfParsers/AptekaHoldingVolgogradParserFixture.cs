using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

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

		[Test]
		public void ParseFarmCenter()
		{
			var document = WaybillParser.Parse("D0036681.DBF");

			Assert.That(document.ProviderDocumentId, Is.EqualTo("Р000036681"));
			Assert.That(document.DocumentDate, Is.EqualTo(new DateTime(2013, 7, 12)));

			var invoice = document.Invoice;
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("Р000036681"));
			Assert.That(invoice.InvoiceDate, Is.EqualTo(new DateTime(2013, 7, 12)));
			Assert.That(invoice.DelayOfPaymentInDays, Is.EqualTo(21));
			Assert.That(invoice.SellerAddress,  Is.EqualTo("392008, Россия, г. Тамбов, Моршанское шоссе, 17 Б"));
			Assert.That(invoice.SellerINN, Is.EqualTo("6832031876"));
			Assert.That(invoice.SellerKPP, Is.EqualTo("683201001"));
			Assert.That(invoice.SellerName, Is.EqualTo("ООО " + '\u0022' + "Фармцентр" + '\u0022'));
			Assert.That(invoice.ShipperInfo, Is.EqualTo("ООО " + '\u0022' + "Фармцентр" + '\u0022' +
				" 392008, Россия, г. Тамбов, Моршанское шоссе, 17 Б"));
			Assert.That(invoice.RecipientName, Is.EqualTo("ООО" + '\u0022' + "Аптека" + '\u0022' +
				"Надежда" + '\u0022'));
			Assert.That(invoice.RecipientId, Is.EqualTo(1193));
			Assert.That(invoice.RecipientAddress, Is.EqualTo("ООО" + '\u0022' + "Аптека" + '\u0022' + "Надежда"
				+ '\u0022' + " Адрес: ,393191,Тамбовская обл.,,г.Котовск,,ул. 9-й пятилетки,7,,"));
			Assert.That(invoice.BuyerId, Is.EqualTo(1193));
			Assert.That(invoice.BuyerName, Is.EqualTo("ООО" + '\u0022' + "Аптека" + '\u0022' + "Надежда" + '\u0022'));
			Assert.That(invoice.BuyerAddress, Is.EqualTo(",393191,Тамбовская обл.,,г.Котовск,,ул. 9-й пятилетки,7,,"));
			Assert.That(invoice.BuyerINN, Is.EqualTo("6820028685"));
			Assert.That(invoice.BuyerKPP, Is.EqualTo("682501001"));

			var line = document.Lines[0];
			Assert.That(line.SerialNumber, Is.EqualTo("10/14/2031"));
			Assert.That(line.Period, Is.EqualTo("01.11.2015"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС IN.ФМ08.Д29959"));
			Assert.That(line.CertificatesDate, Is.EqualTo("  .  ."));
			Assert.That(line.ProducerCostWithoutNDS, Is.Null);
			Assert.That(line.RegistryCost, Is.Null);
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ООО  ОЦКК"));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.EAN13, Is.EqualTo(8901043000472));
			Assert.That(line.Producer, Is.EqualTo("Аджио Фармация Индия"));
			Assert.That(line.Product, Is.EqualTo("Аджисепт паст. с мент. эвкалипт №24"));
			Assert.That(line.Code, Is.EqualTo("16722"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130032/050313/0001408/1"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(38.48));
			Assert.That(line.NdsAmount, Is.EqualTo(7.70));
			Assert.That(line.Amount, Is.EqualTo(76.96));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Unit, Is.EqualTo("упак"));
			Assert.That(line.OrderId, Is.Null);
		}

		[Test]
		public void Parse_regul()
		{
			var doc = WaybillParser.Parse("Рн001075.dbf");
			var line = doc.Lines[0];
			Assert.AreEqual("Атенолол Никомед табл.п.о. 100 мг №30 Nycomed GmbH/Германия/", line.Product);
			Assert.AreEqual(36.19, line.ProducerCostWithoutNDS);
		}
	}
}
