using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class GenezisVrnParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("656522.dbf");
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("656522"));
			Assert.That(doc.DocumentDate.ToString(), Is.EqualTo(DateTime.Now.ToString()));
			var line = doc.Lines[0];
			Assert.That(line.Code, Is.EqualTo("19880"));
			Assert.That(line.Product, Is.EqualTo("БАЛЬЗАМ ВАЛЕНТИНА ДИКУЛЯ 75 МЛ"));
			Assert.That(line.Producer, Is.EqualTo("КОРОЛЕВФАРМ ООО"));
			Assert.That(line.Country, Is.EqualTo("Российская Федерация"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.Period, Is.EqualTo("01.01.2012"));
			Assert.That(line.Certificates, Is.EqualTo("РОССRUПР73В34354"));
			Assert.That(line.SupplierCost, Is.EqualTo(113.73));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(96.38000));
			Assert.That(line.ProducerCost, Is.EqualTo(93.94));
			Assert.That(line.SerialNumber, Is.EqualTo("012010"));
			Assert.That(line.SupplierPriceMarkup, Is.Null);
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.RegistryCost, Is.Null);
		}
	}
}
