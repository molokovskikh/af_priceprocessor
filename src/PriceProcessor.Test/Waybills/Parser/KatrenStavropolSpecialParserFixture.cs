using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;
using System;
using System.Linq;
using PriceProcessor.Test.TestHelpers;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Parser.XmlParsers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class KatrenStavropolSpecialParserFixture
	{
		[Test]
		public void Parse()
		{
			var documentLog = new DocumentReceiveLog { Supplier = new Supplier { Id = 15617 }, }; // код поставщика Катрен-Ставрополь
			Assert.IsTrue(new WaybillFormatDetector().DetectParser(@"..\..\Data\Waybills\457244-14.pld", documentLog) is KatrenStavropolSpecialParser);

			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\457244-14.pld", documentLog);

			Assert.That(doc.Lines.Count, Is.EqualTo(37));

			Assert.That(doc.Invoice.Amount, Is.EqualTo(17577.85));
			Assert.That(doc.Invoice.SellerName, Is.EqualTo("ЗАО НПК \"Катрен\""));
			Assert.That(doc.Invoice.BuyerName.StartsWith("АДЫГЕ-ХАБЛЬ, ИП *Гукев Р.З.*"), Is.EqualTo(true));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("457244-14"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("2015-12-29")));

			Assert.That(doc.Lines[0].EAN13, Is.EqualTo(4601969005493));
			Assert.That(doc.Lines[0].Code, Is.EqualTo("4509761"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("АКРИДЕРМ 0,05% 30,0 МАЗЬ"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Акрихин ХФК ОАО"));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("3840915"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(78.83));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(78.83));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(71.10));
			Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(14.22));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("россия"));
			Assert.That(doc.Lines[0].BillOfEntryNumber, Is.EqualTo(""));
			Assert.That(doc.Lines[0].CertificatesEndDate, Is.EqualTo(Convert.ToDateTime("2019-09-01")));

			var sumWithoutNds = doc.Lines.Sum(x => x.SupplierCostWithoutNDS * x.Quantity);
			var ndsAmount = doc.Lines.Sum(x => x.NdsAmount);
			Assert.That(doc.Invoice.Amount, Is.EqualTo(sumWithoutNds + ndsAmount));
		}
	}
}