using System;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class SiaVrnParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("Р-1873247.DBF");
			var providerDocId = Document.GenerateProviderDocumentId();
			providerDocId = providerDocId.Remove(providerDocId.Length - 1);

			Assert.IsTrue(doc.ProviderDocumentId.StartsWith(providerDocId));
			Assert.That(doc.DocumentDate.ToString(), Is.EqualTo(DateTime.Now.ToString()));

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("2592"));
			Assert.That(line.Product, Is.EqualTo("Аллергодил 140мкг/доза Спрей назал. дозир. 10мл Фл. Б М"));
			Assert.That(line.Producer, Is.EqualTo("MEDA Pharma GmbH & Co. KG"));
			Assert.That(line.Country, Is.EqualTo("Германия"));
			Assert.That(line.SerialNumber, Is.EqualTo("8M075A"));
			Assert.That(line.Period, Is.EqualTo("31.10.2011"));
			Assert.That(line.SupplierCost, Is.EqualTo(183.37));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Certificates, Is.EqualTo("РОСС DE.ФМ08.Д51369"));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(-14.91));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(166.7));
			Assert.That(line.ProducerCost, Is.EqualTo(195.9));
			Assert.That(line.VitallyImportant, Is.False);
		}
	}
}