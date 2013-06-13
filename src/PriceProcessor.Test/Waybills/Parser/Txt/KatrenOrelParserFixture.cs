using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class KatrenOrelParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse(@"..\..\Data\Waybills\288010.txt");

			Assert.That(doc.ProviderDocumentId, Is.EqualTo("288010"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("22.10.2012")));
			Assert.That(doc.Invoice.Amount, Is.EqualTo(5966.86));
			Assert.That(doc.Invoice.NDSAmount, Is.EqualTo(700.01));

			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("29421756"));
			Assert.That(line.Product, Is.EqualTo("ТЕРМОМЕТР WT-03 ЭЛЕКТР СЕМЕЙНЫЙ ВЛАГОЗАЩ"));
			Assert.That(line.Producer, Is.EqualTo("B. Well Limited"));
			Assert.That(line.Country, Is.EqualTo("китай"));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.SupplierCost, Is.EqualTo(75.68));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(75.68));
			Assert.That(line.Nds, Is.EqualTo(0));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130090/260412/0036898/2"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС GB.ИМ04.В07962"));
			Assert.That(line.SerialNumber, Is.EqualTo("1кв2012"));
			Assert.That(line.Period, Is.EqualTo("01.03.2014"));
			Assert.That(line.EAN13, Is.EqualTo("6946159500098"));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Amount, Is.EqualTo(151.36));
		}
	}
}
