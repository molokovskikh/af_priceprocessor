using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class OACXlsParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("3897503_ОАС(order732274).xls");
			Assert.That(document.ProviderDocumentId, Is.EqualTo("91931"));
			Assert.That(document.DocumentDate, Is.EqualTo(DateTime.Parse("02.06.10")));

			Assert.That(document.Lines.Count, Is.EqualTo(7));
			var line = document.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Амлодипин тб 10мг бл пач карт N10x3 Озон РОС"));
			Assert.That(line.Code, Is.Null);
			Assert.That(line.Producer, Is.Null);
			Assert.That(line.RegistryCost, Is.Null);
			Assert.That(line.SupplierCost, Is.EqualTo(66.41));
			Assert.That(line.Quantity, Is.EqualTo(5));
			Assert.That(line.ProducerCostWithoutNDS, Is.Null);
			Assert.That(line.SerialNumber, Is.Null);
			Assert.That(line.Certificates, Is.Null);
			Assert.That(line.Country, Is.Null);
			Assert.That(line.Period, Is.Null);
			Assert.That(line.Nds, Is.Null);
			Assert.That(line.VitallyImportant, Is.Null);
		}
	}
}