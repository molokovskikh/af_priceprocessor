using System;
using System.Data;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NHibernate.Util;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	internal class BellaDonParserFixture
	{
		/// <summary>
		/// Тест для задачи http://redmine.analit.net/issues/35453
		/// </summary>
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("накл-беллаДон-bv006172.DBF");
			Assert.That(document.Parser, Is.EqualTo("BellaDonParser"));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("В0000006172"));
			Assert.That(document.DocumentDate, Is.EqualTo(new DateTime(2015, 06, 04)));

			//проверяем что для разбора данного формата файла подходит только один парсер
			var detector = new WaybillFormatDetector();
			var parsers = detector.GetSuitableParsers(@"..\..\Data\Waybills\r_03-068.DBF", null).ToList();
			Assert.That(parsers.ToList().Count, Is.EqualTo(1));

			var line0 = document.Lines[0];
			Assert.That(line0.Code, Is.EqualTo("SE-091-B010-J03"));
			Assert.That(line0.Product, Is.EqualTo("Гигиенические пеленки Seni Soft Basic 90*60 10 шт."));
			Assert.That(line0.EAN13, Is.EqualTo(5900516692469));
			Assert.That(line0.Quantity, Is.EqualTo(6));
			Assert.That(line0.Country, Is.EqualTo("РОССИЯ"));
			Assert.That(line0.Producer, Is.EqualTo("TZMO S.A."));
			Assert.That(line0.Period, Is.EqualTo("16.10.2016"));
			Assert.That(line0.Nds, Is.EqualTo(10));
			Assert.That(line0.SupplierCost, Is.EqualTo(175.58));
			Assert.That(line0.SupplierCostWithoutNDS, Is.EqualTo(159.62));
			Assert.That(line0.VitallyImportant, Is.False);
			Assert.That(line0.BillOfEntryNumber, Is.Null);
			Assert.That(line0.Certificates, Is.EqualTo("РОСС RU. ИМ09.Д00251"));
			Assert.That(line0.CertificatesDate, Is.EqualTo("03.12.2015"));
			Assert.That(line0.DateOfManufacture, Is.Null);
			Assert.That(line0.SerialNumber, Is.EqualTo("30999/00"));

			var invoice = document.Invoice;
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("В0000006172"));
			Assert.That(invoice.RecipientName, Is.EqualTo("Фармспирт-1"));
			Assert.That(invoice.InvoiceDate, Is.EqualTo(new DateTime(2015, 06, 04)));
		}
	}
}
