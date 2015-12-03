using System;
using System.Linq;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using PriceProcessor.Test.TestHelpers;
using Inforoom.PriceProcessor.Waybills;
using System.IO;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class Original16517ParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("1772.DBF");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("О00001772"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("18/11/2015")));
			Assert.That(doc.Lines.Count, Is.EqualTo(10));

			var invoice = doc.Invoice;
			Assert.That(invoice.Amount, Is.EqualTo(1719.38));
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("О00001772"));
			Assert.That(invoice.RecipientId, Is.EqualTo(792));
			Assert.That(invoice.RecipientName, Is.EqualTo("Здоровый Мир00000792"));

			var line = doc.Lines[0];
			Assert.That(line.Amount, Is.EqualTo(68.25));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10216100/031212/0131825"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ОПС АНО ТЕСТ-С.-ПЕТЕРБУРГ"));
			Assert.That(line.Certificates, Is.EqualTo("ТС RU C-TW.AE45.B.02725"));
			Assert.That(line.CertificatesDate, Is.EqualTo("04.04.2014"));
			Assert.That(line.CertificatesEndDate, Is.EqualTo(DateTime.Parse("03/04/2015")));
			Assert.That(line.Code, Is.EqualTo("2803"));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(6.20));
			Assert.That(line.Producer, Is.EqualTo("ООО \"ТОРИЛЕН\""));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(61.23));
			Assert.That(line.Product, Is.EqualTo("\"Сказка\" Игрушка сборная \"Животные\""));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.SerialNumber, Is.EqualTo("3кв. 2014 года"));
			Assert.That(line.SupplierCost, Is.EqualTo(68.25));

			Assert.That(line.SupplierCost * line.Quantity, Is.EqualTo(line.Amount));
			var s = doc.Lines.Sum(x => x.Amount.GetValueOrDefault());
			Assert.That(s, Is.EqualTo(invoice.Amount));
			var nds = Decimal.Round(100 * line.Amount.Value * line.Nds.Value / (100 + line.Nds.Value));
			Assert.That(nds, Is.EqualTo(100 * line.NdsAmount));
		}

		// для задачи #41265
		[Test]
		public void ChechForPulsFK3996Parser()
		{
			var detector = new WaybillFormatDetector();
			var filePath = "pulse1.dbf";
			if (!File.Exists(filePath))
				filePath = Path.Combine(@"..\..\Data\Waybills\", filePath);
			var parsers = detector.GetSuitableParsers(filePath, null).ToList();
			Assert.That(parsers.Count, Is.EqualTo(1));
			Assert.That(parsers[0].Name, Is.EqualTo("PulsFK3996Parser"));
		}

	}
}