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
	public class Farmacia16947ParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("15105008.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("15105008"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("04/12/2015")));
			Assert.That(doc.Lines.Count, Is.EqualTo(5));

			var invoice = doc.Invoice;
			Assert.That(invoice.InvoiceDate, Is.EqualTo(DateTime.Parse("04/12/2015")));
			Assert.That(invoice.InvoiceNumber, Is.EqualTo("15-130180"));
			Assert.That(invoice.RecipientAddress, Is.EqualTo("РОССИЯ;респ.Мордовия;г.Саранск;ул.Гагарина, д.99 А"));
			Assert.That(invoice.RecipientName, Is.EqualTo("ООО \"Центральная аптека №2\" Апт.3"));

			var line = doc.Lines[0];
			Assert.That(line.Amount, Is.EqualTo(332.25));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ООО Окружной центр сертификации"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ05.Д08298"));
			Assert.That(line.CertificatesDate, Is.EqualTo("28.10.2015"));
			Assert.That(line.CertificatesEndDate, Is.EqualTo(DateTime.Parse("01/11/2019")));
			Assert.That(line.Code, Is.EqualTo("1.0.02588.30092"));
			Assert.That(line.Country, Is.EqualTo("РОССИЯ"));
			Assert.That(line.EAN13, Is.EqualTo(4602565013851));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(30.2045));
			Assert.That(line.Period, Is.EqualTo("01.11.2019"));
			Assert.That(line.Producer, Is.EqualTo("ОАО Синтез Россия"));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(11.04));
			Assert.That(line.Product, Is.EqualTo("Доксициклин 100мг капс №10"));
			Assert.That(line.Quantity, Is.EqualTo(25));
			Assert.That(line.RegistryCost, Is.EqualTo(11.04));
			Assert.That(line.SerialNumber, Is.EqualTo("211015"));
			Assert.That(line.SupplierCost, Is.EqualTo(13.29));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(12.08));
			Assert.That(line.Unit, Is.EqualTo("уп"));
			Assert.That(line.VitallyImportant, Is.EqualTo(true));

			Assert.That(line.SupplierCost * line.Quantity, Is.EqualTo(line.Amount));

		}

	}
}