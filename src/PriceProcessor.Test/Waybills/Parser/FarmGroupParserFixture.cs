using System;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class FarmGroupParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("00013602.DBF");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("00013602"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("17.05.10")));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("С00027133"));
			Assert.That(line.Product, Is.EqualTo("Валериана (эк-т таб. п/о 0,02г №50)"));
			Assert.That(line.Producer, Is.EqualTo("Озон, ООО"));
			Assert.That(line.SerialNumber, Is.EqualTo("161009"));
			Assert.That(line.Period, Is.EqualTo("01.11.2012"));
			Assert.That(line.SupplierCost, Is.EqualTo(6.96));
			Assert.That(line.Quantity, Is.EqualTo(10));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФМ05.д08755"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(3.9d));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(6.3273));
			Assert.That(line.ProducerCost, Is.EqualTo(6.09));
		}
	}
}