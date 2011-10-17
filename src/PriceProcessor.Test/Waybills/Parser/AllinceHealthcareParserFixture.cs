using System;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class AllinceHealthcareParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("n6422353.dbf");
			Assert.That(document.DocumentDate, Is.EqualTo(DateTime.Parse("19.05.2010")));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("СМ-6422353/00"));
			var line = document.Lines[0];
			Assert.That(document.Lines.Count, Is.EqualTo(19));
			Assert.That(line.Code, Is.EqualTo("54"));
			Assert.That(line.Product, Is.EqualTo("Актовегин мазь 5% 20г Австрия"));
			Assert.That(line.Producer, Is.EqualTo("Nycomed Austria GmbH"));
			Assert.That(line.Country, Is.EqualTo("Австрия"));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.Period, Is.EqualTo("01.11.2014"));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(74.10));
			Assert.That(line.SupplierCost, Is.EqualTo(82.17));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(74.70));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(0.81));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Certificates, Is.EqualTo("РОСС.AT.ФМ08.Д72155"));
			Assert.That(line.SerialNumber, Is.EqualTo("930917"));
		}
	}
}