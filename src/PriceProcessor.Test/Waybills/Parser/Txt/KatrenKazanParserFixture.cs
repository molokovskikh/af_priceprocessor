using System;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class KatrenKazanParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse(@"209982.txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(20));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("209982"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("15.10.2011")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("515526"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("АКВАДЕТРИМ 15000МЕ/МЛ 10МЛ ФЛАК КАПЛИ Д/ВН"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Медана Фарма Акционерное Общество"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("польша"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(144.90));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(139.00));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(27.80));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("010811"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.08.2014"));
			Assert.That(doc.Lines[0].BillOfEntryNumber, Is.EqualTo("10130130/2009110020170/3"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС PL.ФМ01.Д02228"));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(145.36));
			Assert.That(doc.Lines[0].Amount, Is.EqualTo(305.80));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(152.90));
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(-4.07));
		}
	}
}
