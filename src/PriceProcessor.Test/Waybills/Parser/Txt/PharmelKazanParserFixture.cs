using System;
using NUnit.Framework;


namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class PharmelKazanParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("000000226220110412.txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(2));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("2262"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("12.04.11")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("124578"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Папазол-УБФ (табл.  уп.контурн.б/яч. 10 ) Уралбиофарм-Россия"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Уралбиофарм"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(30));

			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(3.82));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(4.2));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("31210"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.01.16"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("pocc ru.фм05.д72060"));
			Assert.That(doc.Lines[0].VitallyImportant, Is.EqualTo(false));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.Null);
			Assert.That(doc.Lines[0].RegistryCost, Is.Null);

			Assert.That(doc.Lines[0].NdsAmount, Is.EqualTo(11.46));
			Assert.That(doc.Lines[0].Amount, Is.EqualTo(126.06));

			Assert.That(doc.Lines[1].Code, Is.EqualTo("122566"));
			Assert.That(doc.Lines[1].Product, Is.EqualTo("Феназепама раствор для инъекций 0,1% (р-р д/ин. 0,1% амп. 1 мл [с нож.амп.] кор. 10) Новосибхимфарм-"));
			Assert.That(doc.Lines[1].Producer, Is.EqualTo("Новосибхимфарм"));
			Assert.That(doc.Lines[1].Country, Is.EqualTo("Россия"));
			Assert.That(doc.Lines[1].Quantity, Is.EqualTo(5));

			Assert.That(doc.Lines[1].SupplierCostWithoutNDS, Is.EqualTo(99.96));
			Assert.That(doc.Lines[1].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.Lines[1].SupplierCost, Is.EqualTo(109.96));
			Assert.That(doc.Lines[1].SerialNumber, Is.EqualTo("610610"));
			Assert.That(doc.Lines[1].Period, Is.EqualTo("01.07.12"));
			Assert.That(doc.Lines[1].Certificates, Is.EqualTo("pocc ru.фм10.д65023"));
			Assert.That(doc.Lines[1].VitallyImportant, Is.EqualTo(true));
			Assert.That(doc.Lines[1].ProducerCostWithoutNDS, Is.EqualTo(93));
			Assert.That(doc.Lines[1].RegistryCost, Is.EqualTo(100.44));

			Assert.That(doc.Lines[1].NdsAmount, Is.EqualTo(49.98));
			Assert.That(doc.Lines[1].Amount, Is.EqualTo(549.78));
		}
	}
}