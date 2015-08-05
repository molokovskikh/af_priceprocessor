using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class TTMF_8049_ParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\TTMF_3987_20121212113400376.xml");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("3987"));
			Assert.That(doc.DocumentDate.Value.Date, Is.EqualTo(DateTime.Parse("2012-12-10")));

			Assert.That(doc.Invoice.BuyerName, Is.EqualTo("060 МУСЛЮМОВО"));
			Assert.That(doc.Invoice.BuyerId, Is.EqualTo(60));
			Assert.That(doc.Invoice.StoreName, Is.EqualTo("МедОтдел2"));

			Assert.That(doc.Lines.Count, Is.EqualTo(6));

			Assert.That(doc.Lines[0].Product, Is.EqualTo("Сибазон табл 5мг №20"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Органика РОССИЯ"));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("10212"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("2017-03-01T00:00:00+03:00"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("RU.ФМ10.Д11862"));
			Assert.That(doc.Lines[0].CertificatesDate, Is.EqualTo("28.02.2012"));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(10.34));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].Cipher, Is.EqualTo("I8175"));
			Assert.That(doc.Lines[0].TradeCost, Is.EqualTo(11.15));
			Assert.That(doc.Lines[0].SaleCost, Is.EqualTo(13.08));
			Assert.That(doc.Lines[0].RetailCost, Is.EqualTo(16.69));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(5));
		}
	}
}
