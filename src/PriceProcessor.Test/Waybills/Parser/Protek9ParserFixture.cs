using System;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class Protek9ParserFixture
	{
		[Test, Ignore("Накладные от протека забираются через сервис")]
		public void Parse()
		{
			var document = WaybillParser.Parse("210734_204533_9487972_1.xls");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("9487972-001"));
			Assert.That(document.DocumentDate, Is.EqualTo(DateTime.Parse("18.05.2010")));

			Assert.That(document.Lines.Count, Is.EqualTo(10));
			var line = document.Lines[0];
			Assert.That(line.Product, Is.EqualTo("БАНЕОЦИН МАЗЬ 20Г"));
			Assert.That(line.Producer, Is.EqualTo("Merck KG&Co"));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.SupplierCost, Is.EqualTo(156.15));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(141.95));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.ProducerCost, Is.EqualTo(135.67));
			Assert.That(line.SerialNumber, Is.EqualTo("A0435059;"));
			Assert.That(line.Certificates, Is.EqualTo("POCC AT.ФM08.Д29591;"));
			Assert.That(line.Country, Is.EqualTo("Австрия"));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.VitallyImportant, Is.False);
		}
	}
}