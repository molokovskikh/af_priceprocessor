using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class SiaInternationalParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse(@"Р-6518436.TXT");

			Assert.That(doc.Lines.Count, Is.EqualTo(12));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Р-6518436"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("14.04.2017")));

			Assert.That(doc.Lines[0].Product, Is.EqualTo("Актрапид НМ Пенфилл 100МЕ/мл р-р д/инъекций 3мл Картр. Х5 Б М (R) (ЖНВЛС)"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("ДАНИЯ"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Ново Нордиск А/С"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС DK.МП25.Д68364"));
			Assert.That(doc.Lines[0].CertificateAuthority, Is.EqualTo(null));
			Assert.That(doc.Lines[0].CertificatesDate, Is.EqualTo("30.09.2018"));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("FT64257"));
			Assert.That(doc.Lines[0].CertificatesEndDate, Is.EqualTo(Convert.ToDateTime("30.09.2018")));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(696.38M));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(699.92M));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(768.47M));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(1));
		}
	}
}