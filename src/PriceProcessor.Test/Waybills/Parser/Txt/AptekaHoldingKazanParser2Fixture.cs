using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class AptekaHoldingKazanParser2Fixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("051097.txt");

			Assert.That(doc.Lines.Count, Is.EqualTo(27));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("0000000001051097/0"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("25.09.2012")));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("31662"));
			Assert.That(line.Product, Is.EqualTo("Авамис спрей назальный 27.5мкг/доза 120доз N1 Великобритания"));
			Assert.That(line.Producer, Is.EqualTo("Glaxo Operations UK Limited"));
			Assert.That(line.Country, Is.EqualTo("Великобритания"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(420.53));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(420.53));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.NdsAmount, Is.EqualTo(42.05));
			Assert.That(line.SerialNumber, Is.EqualTo("C577235"));
			Assert.That(line.Period, Is.EqualTo("01.02.2015"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130130/130712/0014129/01"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС GB.ФМ01.Д75373"));
			Assert.That(line.CertificatesDate, Is.EqualTo("12.07.2012"));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.EAN13, Is.EqualTo("4607008131321"));
			Assert.That(line.VitallyImportant, Is.EqualTo(false));
			Assert.That(line.Amount, Is.EqualTo(462.58));
		}
	}
}
