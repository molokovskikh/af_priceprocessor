﻿using System;
using System.Data;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NHibernate.Util;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	internal class TekoSParserFixture : DocumentFixture
	{
		/// <summary>
		/// Тест для задачи http://redmine.analit.net/issues/36689
		/// </summary>
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("55865(2).DBF");
			Assert.That(document.Parser, Is.EqualTo("TekoSParser"));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("ЦБ01-055865"));
			Assert.That(document.DocumentDate, Is.EqualTo(new DateTime(2015, 07, 07)));

			//проверяем что для разбора данного формата файла подходит только один парсер
			var detector = new WaybillFormatDetector();
			var parsers = detector.GetSuitableParsers(@"..\..\Data\Waybills\r_03-068.DBF", null).ToList();
			Assert.That(parsers.ToList().Count, Is.EqualTo(1));

			var line0 = document.Lines[0];
			Assert.That(line0.Product, Is.EqualTo("Медицинский антисепт-кий р-р фл. 95% 100мл"));
			Assert.That(line0.Producer, Is.EqualTo("БиоФармКомбинат/Россия"));
			Assert.That(line0.SerialNumber, Is.EqualTo("070514"));
			Assert.That(line0.Period, Is.EqualTo("01.05.2019"));
			Assert.That(line0.Certificates, Is.EqualTo("POCC RU.ФМ08.Д07626"));
			Assert.That(line0.ProducerCostWithoutNDS, Is.EqualTo(22.43));
			Assert.That(line0.SupplierCostWithoutNDS, Is.EqualTo(13.12));
			Assert.That(line0.Nds, Is.EqualTo(10));
			Assert.That(line0.SupplierCost, Is.EqualTo(14.43));
			Assert.That(line0.Quantity, Is.EqualTo(40));
			Assert.That(line0.CodeCr, Is.EqualTo("24894"));
			Assert.That(line0.VitallyImportant, Is.True);
			Assert.That(line0.NdsAmount, Is.EqualTo(52.47));
			Assert.That(line0.Amount, Is.EqualTo(577.20));
			Assert.That(line0.Unit, Is.EqualTo("уп"));
			Assert.That(line0.BillOfEntryNumber, Is.Null);
			Assert.That(line0.EAN13, Is.EqualTo("4680006520908"));
			var invoice = document.Invoice;
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("55865"));
			Assert.That(invoice.InvoiceDate, Is.EqualTo(new DateTime(2015, 07, 07)));
			Assert.That(invoice.SellerAddress, Is.EqualTo("410065,г.Саратов,2-ой Красноармейский тупик,д.3"));
			Assert.That(invoice.SellerINN, Is.EqualTo("6453064469"));
			Assert.That(invoice.SellerKPP, Is.EqualTo("645301001"));
			Assert.That(invoice.RecipientName, Is.EqualTo("Воронеж Поворино Золотой век ООО"));
			//в связи с переполнением - null
			Assert.That(invoice.RecipientId, Is.Null);
			Assert.That(invoice.ShipperInfo, Is.EqualTo("Воронеж Поворино Золотой век ООО,397350, г.Поворино, ул.Московская, д.53 а"));
			Assert.That(invoice.BuyerId, Is.EqualTo(23067));
			Assert.That(invoice.BuyerName, Is.EqualTo("Воронеж Поворино Золотой век ООО"));
			Assert.That(invoice.BuyerAddress, Is.EqualTo("397350, г.Поворино, ул.Советская, д.85"));
			Assert.That(invoice.BuyerINN, Is.EqualTo("3623008451"));
			Assert.That(invoice.BuyerKPP, Is.EqualTo("362301001"));
			Assert.That(line0.CertificatesEndDate, Is.EqualTo(new DateTime(2019, 05, 01)));
		}

		// http://redmine.analit.net/issues/53845
		[Test]
		public void Parse_GrandCapitalVrn()
		{
			var document = WaybillParser.Parse("6-000486_00660.dbf");

			var invoice = document.Invoice;
			Assert.That(invoice.SellerName, Is.EqualTo("\"ФК Гранд Капитал ВОРОНЕЖ\""));
			Assert.That(invoice.Amount, Is.EqualTo(14236.3));
			Assert.That(invoice.AmountWithoutNDS0, Is.EqualTo(0));
			Assert.That(invoice.AmountWithoutNDS10, Is.EqualTo(12867));
			Assert.That(invoice.AmountWithoutNDS18, Is.EqualTo(70));
			Assert.That(invoice.AmountWithoutNDS, Is.EqualTo(12937));
			Assert.That(invoice.NDSAmount10, Is.EqualTo(1286.7));
			Assert.That(invoice.NDSAmount18, Is.EqualTo(12.6));
			Assert.That(invoice.NDSAmount, Is.EqualTo(1299.3));
			Assert.That(invoice.DelayOfPaymentInDays, Is.EqualTo(7));

			var line = document.Lines[0];
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.DateOfManufacture, Is.EqualTo(new DateTime(2015, 08, 01)));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.RegistryDate, Is.Null);
			Assert.That(line.CertificateAuthority, Is.EqualTo("Формат качества"));
			Assert.That(line.CertificatesDate, Is.EqualTo("16.09.2015"));
			Assert.That(line.ExportCode, Is.EqualTo("2-001200"));
		}

		[Test]
		public void Parse_GrandCapitalVrn_settings()
		{
			var parser = new Inforoom.PriceProcessor.Models.Parser("TekoSParser", appSupplier);
			parser.Add("TTN", "Header_ProviderDocumentId");
			parser.Add("TTN_DATE", "Header_DocumentDate");
			parser.Add("NAME_POST", "Product");
			parser.Add("PRZV_POST", "Producer");
			parser.Add("SERIA", "SerialNumber");
			parser.Add("SGODN", "Period");
			parser.Add("SERT", "Certificates");
			parser.Add("PRCENABNDS", "ProducerCostWithoutNDS");
			parser.Add("PCENA_BNDS", "SupplierCostWithoutNDS");
			parser.Add("NDS", "Nds");
			parser.Add("PCENA_NDS", "SupplierCost");
			parser.Add("KOL_TOV", "Quantity");
			parser.Add("SP_PRD_ID", "Code");
			parser.Add("VT", "VitallyImportant");
			parser.Add("P_NDS_AMNT", "NdsAmount");
			parser.Add("P_AMNT", "Amount");
			parser.Add("UNIT", "Unit");
			parser.Add("BLL_NTR_ID", "BillOfEntryNumber");
			parser.Add("BAR_CODE", "EAN13");
			parser.Add("SERT_END", "CertificatesEndDate");
			parser.Add("GR_CENA", "RegistryCost");
			parser.Add("MAN_DATE", "DateOfManufacture");
			parser.Add("PRZV_CNTR", "Country");
			parser.Add("REG_DATE", "RegistryDate");
			parser.Add("SERT_AUTH", "CertificateAuthority");
			parser.Add("SERT_DATE", "CertificatesDate");

			parser.Add("I_SEL_ADR", "Invoice_SellerAddress");
			parser.Add("I_SEL_INN", "Invoice_SellerINN");
			parser.Add("I_SEL_KPP", "Invoice_SellerKPP");
			parser.Add("I_RES_NAME", "Invoice_RecipientName");
			parser.Add("I_RES_ID", "Invoice_RecipientId");
			parser.Add("I_BU_ID", "Invoice_BuyerId");
			parser.Add("I_BU_NAME", "Invoice_BuyerName");
			parser.Add("I_BU_INN", "Invoice_BuyerINN");
			parser.Add("I_SEL_NAME", "Invoice_SellerName");
			parser.Add("AMNT", "Invoice_Amount");
			parser.Add("AMNT_N_ALL", "Invoice_NDSAmount");
			parser.Add("I_DEL_D", "Invoice_DelayOfPaymentInDays");
			session.Save(parser);

			var ids = new WaybillService().ParseWaybill(new[] { CreateTestLog("6-000486_00660.dbf").Id });
			var document = session.Load<Document>(ids[0]);
			var invoice = document.Invoice;
			Assert.That(invoice.SellerName, Is.EqualTo("\"ФК Гранд Капитал ВОРОНЕЖ\""));
			Assert.That(invoice.Amount, Is.EqualTo(14236.3));
			Assert.That(invoice.AmountWithoutNDS0, Is.EqualTo(0));
			Assert.That(invoice.AmountWithoutNDS10, Is.EqualTo(12867));
			Assert.That(invoice.AmountWithoutNDS18, Is.EqualTo(70));
			Assert.That(invoice.AmountWithoutNDS, Is.EqualTo(12937));
			Assert.That(invoice.NDSAmount10, Is.EqualTo(1286.7));
			Assert.That(invoice.NDSAmount18, Is.EqualTo(12.6));
			Assert.That(invoice.NDSAmount, Is.EqualTo(1299.3));
			Assert.That(invoice.DelayOfPaymentInDays, Is.EqualTo(7));

			var line = document.Lines[0];
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.DateOfManufacture, Is.EqualTo(new DateTime(2015, 08, 01)));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.RegistryDate, Is.Null);
			Assert.That(line.CertificateAuthority, Is.EqualTo("Формат качества"));
			Assert.That(line.CertificatesDate, Is.EqualTo("16.09.2015"));
			Assert.That(line.ExportCode, Is.EqualTo("2-001200"));
		}
	}
}
