using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class Protek28ParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\212305_140089_9101537_001.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(14));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("9101537-001"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("20.04.2010")));
			Assert.That(document.Lines[0].Code, Is.EqualTo("17693"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("ДЖУНГЛИ ПОЛИВИТАМИНЫ С МИНЕРАЛЬНЫМИ ДОБАВКАМИ ТАБ. №30"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("Sagmel Inc."));
			Assert.That(document.Lines[0].Country, Is.EqualTo("США"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(127.640));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(116.040));
			Assert.That(document.Lines[0].ProducerCost, Is.EqualTo(113.760));
			Assert.That(document.Lines[0].ProducerCost, Is.LessThanOrEqualTo(document.Lines[0].SupplierCost));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.LessThanOrEqualTo(document.Lines[0].SupplierCost));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.07.2012"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(10));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("POCC US.ФM08.Д15837"));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("093220"));

			Assert.That(document.Lines[2].VitallyImportant, Is.True);
		}
	}
}
