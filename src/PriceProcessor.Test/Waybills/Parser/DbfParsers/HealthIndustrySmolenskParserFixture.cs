using System;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class HealthIndustrySmolenskParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"0005716.dbf");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("IZ-0005716"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("20.09.2011")));
			Assert.That(document.Lines.Count, Is.EqualTo(6));

			Assert.That(document.Lines[0].EAN13, Is.EqualTo(4601669000385));
			Assert.That(document.Lines[0].Code, Is.EqualTo("8153"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("Аскофен-П (тбл №10) Фармстандарт Лексредства ОАО Россия"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(6));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(7.87));
			Assert.That(document.Lines[0].Amount, Is.EqualTo(47.22));
			Assert.That(document.Lines[0].Nds, Is.EqualTo(10));
			Assert.That(document.Lines[0].NdsAmount, Is.EqualTo(4.29));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(7.15));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("190111"));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.02.2013"));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("POCC RU.ФM05.Д79901"));
			Assert.That(document.Lines[0].BillOfEntryNumber, Is.Null);
			Assert.That(document.Lines[3].BillOfEntryNumber, Is.EqualTo("10005022/080211/0005361/1"));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Россия"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Фармстандарт Лексредства ОАО"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[4].VitallyImportant, Is.True);
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0.00));
			Assert.That(document.Lines[4].RegistryCost, Is.EqualTo(25.81));
		}
	}
}