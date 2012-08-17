using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	public class ImperiaFarmaSpecialParser2Fixture
	{
		[Test]
		public void Parse()
		{
			var documentLog = new DocumentReceiveLog { Supplier = new Supplier { Id = 74u }, }; // код поставщика Империя-Фарма 
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\KZ069425_корр.txt", documentLog) is ImperiaFarmaSpecialParser2);
			var doc = WaybillParser.Parse("KZ069425_корр.txt", documentLog);
			Assert.That(doc.Lines.Count, Is.EqualTo(1));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("КЗ069425"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("20.12.2011"));
			Assert.That(doc.Lines[0].Code, Is.EqualTo("305"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Панзинорм форте 20000 №30"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("КРКА д.д. Ново место"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Словения"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(4));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(104.76));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(99.57));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(39.83));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("N82575"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.05.2014"));
			Assert.That(doc.Lines[0].BillOfEntryNumber, Is.EqualTo("10130032/160911/0004842/1"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("POCC.SI.ФМ08.Д15539"));
			Assert.That(doc.Lines[0].CertificatesDate, Is.EqualTo("16.09.2011"));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(104.94));
			Assert.That(doc.Lines[0].Amount, Is.EqualTo(438.12));
			Assert.That(doc.Lines[0].VitallyImportant, Is.True);
		}
	}
}