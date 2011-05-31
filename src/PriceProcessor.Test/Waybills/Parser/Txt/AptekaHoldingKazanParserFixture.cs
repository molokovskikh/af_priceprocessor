using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class AptekaHoldingKazanParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("969533.txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(3));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("00000969533"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("17.12.10")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("26781"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Азитрокс капс. 500мг N3 Россия"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Фармстандарт- Лексредства, г. Курск"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(4));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(179));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(167.99));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("120710"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.08.12"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("POCC RU.ФМ05.Д69302"));
			Assert.That(doc.Lines[0].CertificatesDate, Is.EqualTo("03.08.10"));

			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(193.32));
			Assert.That(doc.Lines[0].VitallyImportant, Is.EqualTo(true));
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(-6.15));

			Assert.That(doc.Lines[1].SupplierPriceMarkup, Is.EqualTo(0));
			Assert.That(doc.Lines[2].SupplierPriceMarkup, Is.EqualTo(0));
		}
	}
}
