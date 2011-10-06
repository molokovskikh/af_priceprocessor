using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class PokrevskySpecialParserFixture
	{
		[Test]
		public void Parse()
		{
			var documentLog = new DocumentReceiveLog { Supplier = new Supplier { Id = 11427 } }; // код поставщика ИП Покревский
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\541_POKR.dbf", documentLog) is PokrevskySpecialParser);
			var document = WaybillParser.Parse(@"541_POKR.dbf", documentLog);
			Assert.That(document.Lines.Count, Is.EqualTo(16));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("541"));
			Assert.That(document.DocumentDate.Value.ToShortDateString(), Is.EqualTo("13.09.2011"));
			Assert.That(document.Lines[0].Code, Is.EqualTo("2426"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Козинак 4злака \"Арахис с изюмом\"100г"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("ТПО Диал"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(26.32));
			Assert.That(document.Lines[0].SupplierPriceMarkup, Is.EqualTo(3.00));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(0));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(0.00));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(27.11));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(54.22));
			Assert.That(document.Lines[0].SerialNumber, Is.Null);
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("АЯ42.Н27564"));
		}
	}
}
