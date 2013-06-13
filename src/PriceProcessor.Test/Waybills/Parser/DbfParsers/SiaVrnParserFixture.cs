using System;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class SiaVrnParserFixture
	{
		[Test]
		public void Parse_with_native()
		{
			var doc = WaybillParser.Parse("Р-1696007-1.DBF");
			var line = doc.Lines[0];
			Assert.AreEqual("113150", line.Code);
			Assert.AreEqual(127.46, line.ProducerCostWithoutNDS);
		}

		[Test]
		public void Parse()
		{
			DateTime dt1 = DateTime.Now;
			var doc = WaybillParser.Parse("Р-1873247.DBF");
			DateTime dt2 = DateTime.Now;
			var providerDocId = Document.GenerateProviderDocumentId();
			providerDocId = providerDocId.Remove(providerDocId.Length - 1);

			Assert.IsFalse(doc.ProviderDocumentId.StartsWith(providerDocId));
			Assert.That(doc.DocumentDate, Is.GreaterThanOrEqualTo(dt1));
			Assert.That(doc.DocumentDate, Is.LessThanOrEqualTo(dt2));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("Р-1873247"));

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
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(195.9));
			Assert.That(line.VitallyImportant, Is.False);
		}
	}
}