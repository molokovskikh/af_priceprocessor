using System;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	class MedlineShustovaParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse(@"ЗЗН5036023 (1).TXT");
			Assert.That(doc.Lines.Count, Is.EqualTo(1));

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("17282"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("26.04.2017")));

			Assert.That(doc.Invoice.Amount, Is.EqualTo(1784.00));
			Assert.That(doc.Invoice.SellerINN, Is.EqualTo("3665012499"));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("566017"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Molimed Classic maxi арт.168587~прокладки д/больных недерж.мочи пак.ПЭ 28~ООО \"Пауль Хартманн\" РОССИЯ"));
			Assert.That(doc.Lines[0].VitallyImportant, Is.EqualTo(false));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("600101350"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("04.06.2021"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС RU ИМ41 Д07095"));
			Assert.That(doc.Lines[0].CertificatesEndDate, Is.EqualTo(Convert.ToDateTime("26.09.2019")));
			Assert.That(doc.Lines[0].CertificateAuthority, Is.EqualTo("Орган по сертификации продукции ООО \"Центр сертификации и декларирования\""));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));

			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(303.80));
			Assert.That(doc.Lines[0].Amount, Is.EqualTo(1784.00));

		}
	}
}
