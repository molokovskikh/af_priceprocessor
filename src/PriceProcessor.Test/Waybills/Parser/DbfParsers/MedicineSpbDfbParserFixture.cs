using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class MedicineSpbDfbParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("3902349_Медицина(0110403).dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("РН-0110403"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("03/06/2010")));
			Assert.That(doc.Lines.Count, Is.EqualTo(2));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("3203"));
			Assert.That(line.Product, Is.EqualTo("Нимулид гель трансдерм. 1% 30г"));
			Assert.That(line.Producer, Is.EqualTo("Панацея Биотек Лтд."));
			Assert.That(line.Country, Is.EqualTo("Индия"));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.12.2012"));
			Assert.That(line.Certificates, Is.EqualTo("РОССINФМ08Д57407"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(93.00));
			Assert.That(line.SupplierCost, Is.EqualTo(102.30));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(84.55));
			Assert.That(line.SerialNumber, Is.EqualTo("6059090"));
			Assert.That(line.RegistryCost, Is.EqualTo(0.00));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(9.99));
			Assert.That(line.VitallyImportant, Is.False);
		}
	}
}