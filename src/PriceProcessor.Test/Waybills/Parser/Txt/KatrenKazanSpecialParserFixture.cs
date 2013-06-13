using System;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class KatrenKazanSpecialParserFixture
	{
		[Test]
		public void Parse()
		{
			var documentLog = new DocumentReceiveLog { Supplier = new Supplier { Id = 2754 }, }; // код поставщика "Катрен" (Казань)
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\218817.txt", documentLog) is KatrenKazanSpecialParser);

			var doc = WaybillParser.Parse(@"218817.txt", documentLog);
			Assert.That(doc.Lines.Count, Is.EqualTo(12));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("218817"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("27.10.2011")));

			Assert.That(doc.Lines[4].Code, Is.EqualTo("379132"));
			Assert.That(doc.Lines[4].Product, Is.EqualTo("ДЕКАРИС 0,05 N2 ТАБЛ"));
			Assert.That(doc.Lines[4].Producer, Is.EqualTo("Гедеон Рихтер ОАО/Гедеон Рихтер Румыния А.О."));
			Assert.That(doc.Lines[4].Country, Is.EqualTo("венгрия"));
			Assert.That(doc.Lines[4].Quantity, Is.EqualTo(2));
			Assert.That(doc.Lines[4].ProducerCostWithoutNDS, Is.EqualTo(50.28));
			Assert.That(doc.Lines[4].SupplierCostWithoutNDS, Is.EqualTo(51.60));
			Assert.That(doc.Lines[4].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[4].NdsAmount, Is.EqualTo(10.32));
			Assert.That(doc.Lines[4].SerialNumber, Is.EqualTo("F14010F"));
			Assert.That(doc.Lines[4].Period, Is.EqualTo("01.04.2016"));
			Assert.That(doc.Lines[4].BillOfEntryNumber, Is.EqualTo("10130032/101011/0005403/24"));
			Assert.That(doc.Lines[4].Certificates, Is.EqualTo("РОСС HU.ФМ08.Д19429"));
			Assert.That(doc.Lines[4].CertificatesDate, Is.EqualTo("27.09.2011"));
			Assert.That(doc.Lines[4].RegistryCost, Is.EqualTo(50.42));
			Assert.That(doc.Lines[4].EAN13, Is.EqualTo("5997001380338"));
			Assert.That(doc.Lines[4].VitallyImportant, Is.True);
			Assert.That(doc.Lines[4].Amount, Is.EqualTo(113.52));
		}
	}
}