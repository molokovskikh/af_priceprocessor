using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class AptekaHoldingLipetskParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("69565_0.dbf");

			Assert.That(document.Lines.Count, Is.EqualTo(18));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("00000469565/0"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("02.06.2011"));

			var invoice = document.Invoice;
			Assert.That(invoice, Is.Not.Null);
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("00000469565/0"));
			Assert.That(invoice.InvoiceDate.Value.ToShortDateString(), Is.EqualTo("02.06.2011"));
			Assert.That(invoice.SellerName, Is.EqualTo("Закрытое акционерное общество <АПТЕКА-ХОЛДИНГ>, ЗАО <АПТЕКА-ХОЛДИНГ>"));
			Assert.That(invoice.SellerAddress, Is.EqualTo("117042, г.Москва, ул.Горчакова, дом 1"));
			Assert.That(invoice.SellerINN, Is.EqualTo("7729350817"));
			Assert.That(invoice.SellerKPP, Is.EqualTo("623403001"));
			Assert.That(invoice.ShipperInfo, Is.EqualTo("филиал ЗАО \"Аптека-Холдинг\" в г. Рязани, Адрес:390013,г. Рязань,Товарный двор станции Рязань-2,строение 31"));
			Assert.That(invoice.RecipientAddress, Is.EqualTo("ОГУП Липецкфармация, аптека, пр. Победы, д.59 \"а\", помещение №2, 398024, г. Липецк, пр. Победы, д.59 \"а\", помещение №2"));
			Assert.That(invoice.PaymentDocumentInfo, Is.EqualTo("0"));
			Assert.That(invoice.BuyerName, Is.EqualTo("Областное государственное унитарное предприятие \"Липецкфармация\""));
			Assert.That(invoice.BuyerAddress, Is.EqualTo("398043, г.Липецк, ул.Гагарина, д.113"));
			Assert.That(invoice.BuyerINN, Is.EqualTo("4826022196"));
			Assert.That(invoice.BuyerKPP, Is.EqualTo("482601001"));
			Assert.That(invoice.AmountWithoutNDS0, Is.EqualTo(0.00));
			Assert.That(invoice.AmountWithoutNDS10, Is.EqualTo(12987.26));
			Assert.That(invoice.NDSAmount10, Is.EqualTo(1298.76));
			Assert.That(invoice.Amount10, Is.EqualTo(14286.02));
			Assert.That(invoice.AmountWithoutNDS18, Is.EqualTo(0.00));
			Assert.That(invoice.NDSAmount18, Is.EqualTo(0.00));
			Assert.That(invoice.Amount18, Is.EqualTo(0.00));
			Assert.That(invoice.AmountWithoutNDS, Is.EqualTo(12987.26));
			Assert.That(invoice.NDSAmount, Is.EqualTo(1298.76));
			Assert.That(invoice.Amount, Is.EqualTo(14286.02));
			Assert.IsTrue(invoice.IsSupplierAmount);

			var line = document.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Алфлутоп амп 10мг/мл 1мл N10 Румыния"));
			Assert.That(line.Unit, Is.EqualTo("уп."));
			Assert.That(line.Quantity, Is.EqualTo(5));
			Assert.That(line.Producer, Is.EqualTo("Biotehnos S.A."));
			Assert.That(line.SupplierCost, Is.EqualTo(1087.13));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(988.30));
			Assert.That(line.ExciseTax, Is.EqualTo(0.00));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0.00));
			Assert.That(line.RegistryCost, Is.EqualTo(0.00));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(988.30));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(494.15));
			Assert.That(line.Amount, Is.EqualTo(5435.65));
			Assert.That(line.SerialNumber, Is.EqualTo("3121110"));
			Assert.That(line.Period, Is.EqualTo("01.10.2013"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС.RO.ФМ08.Д61710"));
			Assert.That(line.Country, Is.EqualTo("Румыния"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10126110/271210/0023082/001"));
			Assert.That(line.CertificatesDate, Is.EqualTo("24.12.2010"));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.EAN13, Is.EqualTo("5944700100019"));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(AptekaHoldingLipetskParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\69565_0.dbf")));
		}
	}
}