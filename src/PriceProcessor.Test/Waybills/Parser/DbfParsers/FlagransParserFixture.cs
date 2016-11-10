using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inforoom.PriceProcessor.Waybills;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	internal class FlagransParserFixture
	{
		/// <summary>
		/// Тест для задачи http://redmine.analit.net/issues/56551
		/// </summary>
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("1230.dbf");
			Assert.That(document.Parser, Is.EqualTo("FlagransParser"));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("ФЛА00001230"));
			Assert.That(document.DocumentDate, Is.EqualTo(new DateTime(2016, 11, 03)));

			var detector = new WaybillFormatDetector();
			var parsers = detector.GetSuitableParsers(@"..\..\Data\Waybills\1230.dbf", null).ToList();
			Assert.That(parsers.ToList().Count, Is.EqualTo(1));

			var line0 = document.Lines[0];
			Assert.That(line0.Code, Is.EqualTo("3312"));
			Assert.That(line0.RegistryCost, Is.Null);
			Assert.That(line0.Product, Is.EqualTo("Крем - гель для проблемной кожи. Матирующий эффект  50 мл."));
			Assert.That(line0.Quantity, Is.EqualTo(1.0000));
			Assert.That(line0.SupplierCost, Is.EqualTo(385.00));
			Assert.That(line0.SupplierCostWithoutNDS, Is.EqualTo(385.00));
			Assert.That(line0.Producer, Is.EqualTo("Россия"));
			Assert.That(line0.Country, Is.EqualTo("Россия"));
			Assert.That(line0.ProducerCostWithoutNDS, Is.EqualTo(385.00));
			Assert.That(line0.VitallyImportant, Is.Null);
			Assert.That(line0.Period, Is.Null);
			Assert.That(line0.Nds, Is.EqualTo(0));
			Assert.That(line0.SerialNumber, Is.EqualTo("RU.Д-RU.ПК08.В.00726"));

			var invoice = document.Invoice;
			Assert.That(invoice.Amount, Is.EqualTo(385.00));
			Assert.That(invoice.NDSAmount, Is.EqualTo(385.00));
		}
	}
}
