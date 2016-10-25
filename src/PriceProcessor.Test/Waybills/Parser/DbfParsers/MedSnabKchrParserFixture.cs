using System;
using System.Linq;
using Inforoom.PriceProcessor.Waybills;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	internal class MedSnabKchrParserFixture
	{
		/// <summary>
		/// Тест для задачи http://redmine.analit.net/issues/55941
		/// </summary>
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("66426.dbf");
			Assert.That(document.Parser, Is.EqualTo("MedSnabKchrParser"));
			Assert.That(document.ProviderDocumentId.Trim(), Is.EqualTo("66426"));
			Assert.That(document.DocumentDate, Is.EqualTo(new DateTime(2016, 09, 27)));

			var detector = new WaybillFormatDetector();
			var parsers = detector.GetSuitableParsers(@"..\..\Data\Waybills\66426.dbf", null).ToList();
			Assert.That(parsers.ToList().Count, Is.EqualTo(1));

			var line0 = document.Lines[0];
			Assert.That(line0.Code, Is.EqualTo("18036,0000"));
			Assert.That(line0.Product, Is.EqualTo("Зодак капли 10мг/мл 20мл"));
			Assert.That(line0.EAN13, Is.EqualTo("8594739055209"));
			Assert.That(line0.Quantity, Is.EqualTo(2.0000));
			Assert.That(line0.Producer, Is.EqualTo("Зентива/Лечива"));
			Assert.That(line0.SupplierCost, Is.EqualTo(171.1700));
			Assert.That(line0.SupplierCostWithoutNDS, Is.EqualTo(155.6100));
			Assert.That(line0.VitallyImportant, Is.True);
			Assert.That(line0.BillOfEntryNumber, Is.EqualTo("10113100/190416/0019473/5"));
			Assert.That(line0.Certificates, Is.EqualTo("РОСС CZ.ФМ08.Д21991"));
			Assert.That(line0.CertificatesEndDate, Is.EqualTo(new DateTime(2019, 02, 28)));
			Assert.That(line0.SerialNumber, Is.EqualTo("3130316"));

			var invoice = document.Invoice;
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("170560,0000"));
			Assert.That(invoice.RecipientName, Is.Null);
			Assert.That(invoice.InvoiceDate, Is.Null);
		}
	}
}
