using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class OriolaVoronezhSpecialParserFixture
	{
		[Test]
		public void Parse()
		{
			var documentLog = new DocumentReceiveLog { Supplier = new Supplier { Id = 7 } }; // код поставщика Ориола (Воронеж)
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\Reon_AX_Comp_Nzak.dbf", documentLog) is OriolaVoronezhSpecialParser);
			var document = WaybillParser.Parse(@"Reon_AX_Comp_Nzak.dbf", documentLog);
			Assert.That(document.Lines.Count, Is.EqualTo(29));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("1006044"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("27.06.2011"));
			Assert.That(document.Lines[0].Code, Is.EqualTo("65313"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Аквалор беби д/дет 15мл"));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("SM052(0311)"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(66.40));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(6.64));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("РОСС.FR.ИМ25.А01079"));
			Assert.That(document.Lines[0].CertificatesDate, Is.EqualTo("25.04.2008"));
			Assert.That(document.Lines[0].Period, Is.Null);
			Assert.That(document.Lines[0].Country, Is.EqualTo("Франция"));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0.00));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(69.09));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(-3.89));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("YS LAB"));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(73.04));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(73.04));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[0].EAN13, Is.EqualTo("3582910130062"));
			Assert.That(document.Lines[0].BillOfEntryNumber, Is.EqualTo("10130032/080411/0001507/1"));
			Assert.That(document.Lines[0].OrderId, Is.EqualTo(1));
		}
	}
}