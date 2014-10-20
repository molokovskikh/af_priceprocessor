using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class KatrenVoronezhParserFixture
	{
		[Test]
		public void Parse()
		{
			Assert.IsTrue(KatrenVoronezhParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\241417.dbf")));
			var document = WaybillParser.Parse("241417.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(16));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("241417"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("20.10.2011"));

			var invoice = document.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("241417"));
			Assert.That(invoice.InvoiceDate.Value.ToShortDateString(), Is.EqualTo("21.10.2011"));
			Assert.That(invoice.SellerName, Is.EqualTo("Филиал ЗАО НПК \"Катрен\" в г.Воронеж"));
			Assert.That(invoice.SellerAddress, Is.EqualTo("394065, Россия, г. Воронеж, пр-т Патриотов д. 57а"));
			Assert.That(invoice.SellerINN, Is.EqualTo("5408130693"));
			Assert.That(invoice.SellerKPP, Is.EqualTo("366502001"));
			Assert.That(invoice.ShipperInfo, Is.EqualTo("394065, Россия, г. Воронеж, пр-т Патриотов д. 57а"));
			Assert.That(invoice.RecipientAddress, Is.EqualTo(", г. Воронеж, ул. Димитрова, д. 127"));
			Assert.That(invoice.PaymentDocumentInfo, Is.Null);
			Assert.That(invoice.BuyerName, Is.EqualTo("ВОРОНЕЖ, ИП *Воронова М.В.*"));
			Assert.That(invoice.BuyerAddress, Is.EqualTo("г. Воронеж, Набережная Авиастроителей, дом 18 кв. 360"));
			Assert.That(invoice.BuyerINN, Is.EqualTo("366300341432"));
			Assert.That(invoice.BuyerKPP, Is.Null);
			Assert.That(invoice.AmountWithoutNDS0, Is.EqualTo(0.00));
			Assert.That(invoice.AmountWithoutNDS10, Is.EqualTo(572.60));
			Assert.That(invoice.NDSAmount10, Is.EqualTo(57.26));
			Assert.That(invoice.Amount10, Is.EqualTo(629.86));
			Assert.That(invoice.AmountWithoutNDS18, Is.EqualTo(2755.09));
			Assert.That(invoice.NDSAmount18, Is.EqualTo(495.92));
			Assert.That(invoice.Amount18, Is.EqualTo(3251.01));
			Assert.That(invoice.AmountWithoutNDS, Is.EqualTo(3327.69));
			Assert.That(invoice.NDSAmount, Is.EqualTo(553.18));
			Assert.That(invoice.Amount, Is.EqualTo(3880.87));

			var line = document.Lines[0];
			Assert.That(line.Product, Is.EqualTo("СТОПАНГИН 2А N24 ТАБЛ Д/РАССАС /МЯТА/"));
			Assert.That(line.Unit, Is.EqualTo("шт."));
			Assert.That(line.Quantity, Is.EqualTo(7));
			Assert.That(line.Producer, Is.EqualTo("Рафа Лабораториз Лтд"));
			Assert.That(line.SupplierCost, Is.EqualTo(89.98));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(81.80));
			Assert.That(line.ExciseTax, Is.Null);
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0.00));
			Assert.That(line.RegistryCost, Is.EqualTo(0.00));
			Assert.That(line.ProducerCost, Is.EqualTo(89.98));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(81.80));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(57.26));
			Assert.That(line.Amount, Is.EqualTo(629.86));
			Assert.That(line.SerialNumber, Is.EqualTo("102437"));
			Assert.That(line.Period, Is.EqualTo("01.04.2014"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС IL.ФМ08.Д61690"));
			Assert.That(line.Country, Is.EqualTo("израиль"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130032/170611/0002992/5"));
			Assert.That(line.CertificatesDate, Is.EqualTo("15.06.2011"));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.EAN13, Is.EqualTo("7290008016766"));
			Assert.That(line.OrderId, Is.EqualTo(297967));
		}

		/// <summary>
		/// Новые данные от задачи
		/// http://redmine.analit.net/issues/28233
		/// </summary>
		[Test]
		public void Parse2()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\369151.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(30));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("369151"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("09.10.2014")));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("35475160"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10206090/080314/0001177/01"));
			Assert.That(line.EAN13, Is.EqualTo("6414100009230"));
			Assert.That(line.CountryCode, Is.EqualTo("246"));
			Assert.That(line.UnitCode, Is.EqualTo("778"));
		}
	}
}