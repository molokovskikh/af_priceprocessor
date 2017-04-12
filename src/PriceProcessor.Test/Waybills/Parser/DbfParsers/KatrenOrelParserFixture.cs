using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class KatrenOrelParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\247680.dbf");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("247680"));
			Assert.That(document.DocumentDate, Is.EqualTo(new DateTime(2012, 09, 17)));

			var invoice = document.Invoice;
			Assert.That(invoice.Amount, Is.EqualTo(6323.77));
			Assert.That(invoice.NDSAmount, Is.EqualTo(723.45));
			Assert.That(invoice.AmountWithoutNDS, Is.EqualTo(5600.32));
			Assert.That(invoice.RecipientId, Is.EqualTo(25160254));

			var line = document.Lines[0];
			Assert.That(line.EAN13, Is.EqualTo(4607018261599));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(137.87));
			Assert.That(line.SupplierCost, Is.EqualTo(140.69));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(127.9));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.SerialNumber, Is.EqualTo("050712"));
			Assert.That(line.Period, Is.EqualTo("01.07.2017"));
			Assert.That(line.Product, Is.EqualTo("АЗАФЕН 0,025 N50 ТАБЛ"));
			Assert.That(line.Country, Is.EqualTo("россия"));
			Assert.That(line.Producer, Is.EqualTo("МАКИЗ-ФАРМА,ЗАО"));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.VitallyImportant, Is.EqualTo(true));
			Assert.That(line.RegistryCost, Is.EqualTo(137.87));
			Assert.That(line.RegistryDate, Is.EqualTo(new DateTime(2010, 5, 5)));
			Assert.That(line.BillOfEntryNumber, Is.Null);
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ01.Д99197"));
			Assert.That(line.CertificatesDate, Is.EqualTo("01.07.2017"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("ФГБУ \"ЦЭККМП\"Росздравнадзора"));
		}

		[Test]
		public void Parse2()
		{
			var doc = WaybillParser.Parse("накл-катрен-орел-186246.dbf");

			Assert.AreEqual("14.05.2014 0:00:00", doc.DocumentDate.ToString());
			Assert.AreEqual("186246", doc.ProviderDocumentId);
			Assert.AreEqual(13, doc.Lines.Count);
			var line = doc.Lines[0];
			Assert.AreEqual("ВАЛЬСАКОР 0,16 N28 ТАБЛ П/О", line.Product);
			Assert.AreEqual("КРКА, д.д., Ново место", line.Producer);
			Assert.AreEqual("22994749", line.CodeCr);
			Assert.AreEqual(3838989551223, line.EAN13);
		}
	}
}
