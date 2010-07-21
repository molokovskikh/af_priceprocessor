using System;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class BiolainFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("4048371_Биолайн(10283).txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(14));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("10283"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("08.07.2010")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("50199"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Левосин~мазь~туба40г N1~Нижфарм"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo("Нижфарм"));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("RU"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(3));
			Assert.That(doc.Lines[0].ProducerCost, Is.EqualTo(52.54));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(56.26));
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(51.14));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(doc.Lines[0].SerialNumber, Is.EqualTo("241209"));
			Assert.That(doc.Lines[0].Period, Is.EqualTo("01.01.2012"));
			Assert.That(doc.Lines[0].Certificates, Is.EqualTo("РОСС RU ФМ01 Д26574"));
			Assert.That(doc.Lines[0].RegistryCost, Is.Null);
			Assert.That(doc.Lines[0].VitallyImportant, Is.Null);
			Assert.That(doc.Lines[0].SupplierPriceMarkup, Is.EqualTo(15.35));
		}
	}
}