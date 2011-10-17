using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class Shafiev_KalugaParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("gigiena0000018791.txt");
			Assert.That(doc.Lines.Count, Is.EqualTo(29));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("18791"));
			Assert.That(doc.DocumentDate, Is.EqualTo(Convert.ToDateTime("06.10.10")));

			Assert.That(doc.Lines[0].Code, Is.EqualTo("12332"));
			Assert.That(doc.Lines[0].Product, Is.EqualTo("Аспиратор для носа Canpol"));
			Assert.That(doc.Lines[0].Producer, Is.EqualTo(""));
			Assert.That(doc.Lines[0].Country, Is.EqualTo("Китай"));
			Assert.That(doc.Lines[0].Quantity, Is.EqualTo(5));
			Assert.That(doc.Lines[0].SupplierCost, Is.EqualTo(48.98));
			Assert.That(doc.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(41.1));           //По описанию тут цена производителя без НДС
			Assert.That(doc.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(41.51));
			Assert.That(doc.Lines[0].Nds.Value, Is.EqualTo(18));

		}
	}
}
