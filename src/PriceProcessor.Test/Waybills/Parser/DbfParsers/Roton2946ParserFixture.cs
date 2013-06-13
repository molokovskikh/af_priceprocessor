using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class Roton2946ParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("16.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("16"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("03.08.2011")));

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("1995"));
			Assert.That(line.Product, Is.EqualTo("БПО \"ТРИВЕС\" Т-1337 №4-M"));
			Assert.That(line.Producer, Is.EqualTo("ООО \"ТД \"ТРИВЕС СПБ\""));
			Assert.That(line.Country, Is.EqualTo("Российская Федерация"));
			Assert.That(line.BillOfEntryNumber, Is.Null);
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Nds, Is.EqualTo(0));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(677));
			Assert.That(line.SupplierCost, Is.EqualTo(677));
			Assert.That(line.Amount, Is.EqualTo(677));
			Assert.That(line.NdsAmount, Is.EqualTo(0));
			Assert.That(line.SerialNumber, Is.Null);
			Assert.That(line.Period, Is.EqualTo("31.12.2015"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.МЕ95.Д00698"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("Тест-С-Петербург"));

			var invoice = doc.Invoice;
			Assert.That(invoice.ShipperInfo, Is.EqualTo("ООО \"Ротон\""));
			Assert.That(invoice.BuyerName, Is.EqualTo("Липецкфармация ОГУП"));
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("24"));
			Assert.That(invoice.InvoiceDate, Is.EqualTo(DateTime.Parse("03.08.2011")));
		}
	}
}
