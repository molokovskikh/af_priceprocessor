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
			Assert.IsTrue(WaybillParser.GetParserType(@"..\..\Data\Waybills\KZ034888.txt", documentLog) is ImperiaFarmaSpecialParser2);
			var doc = WaybillParser.Parse("KZ034888.txt", documentLog);
			Assert.That(doc.Lines.Count, Is.EqualTo(1));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("КЗ034888"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("07.10.2011"));
			Assert.That(doc.Lines[0].Code, Is.EqualTo("32574"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Рингер 400мл"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Эском ОАО НПК"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(2));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(32.60));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(32.76));
			Assert.That(doc.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(6.55));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("120511"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.05.2013"));
			Assert.That(doc.Lines[0].BillOfEntryNumber, Is.Null);
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС.RU.ФМ01.Д79653"));
			Assert.That(doc.Lines[0].CertificatesDate, Is.EqualTo("07.07.2011"));
			Assert.That(doc.Lines[0].RegistryCost, Is.EqualTo(32.60));
			Assert.That(doc.Lines[0].Amount, Is.EqualTo(72.08));
		}
	}
}
