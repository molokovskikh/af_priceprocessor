using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class PulsFKParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("3905255_ПУЛЬС ФК(00204995).dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("00204995"));
			Assert.That(doc.DocumentDate, Is.EqualTo(DateTime.Parse("03/06/2010")));
			Assert.That(doc.Lines.Count, Is.EqualTo(6));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("11638"));
			Assert.That(line.Product, Is.EqualTo("Бетагистин табл. 8 мг х30"));
			Assert.That(line.Producer, Is.EqualTo("Канонфарма продакшн"));
			Assert.That(line.Country, Is.EqualTo("РОССИЯ"));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Nds, Is.EqualTo(10));
			Assert.That(line.Period, Is.EqualTo("01.04.2012"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ФM08.Д84457"));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(102.00));
			Assert.That(line.SupplierCost, Is.EqualTo(112.20));
			Assert.That(line.ProducerCost, Is.EqualTo(111.16));
			Assert.That(line.SerialNumber, Is.EqualTo("010310"));
			Assert.That(line.RegistryCost, Is.EqualTo(122.11));
			Assert.That(line.SupplierPriceMarkup, Is.Null);
			Assert.That(line.VitallyImportant, Is.True);
		}
	}
}
