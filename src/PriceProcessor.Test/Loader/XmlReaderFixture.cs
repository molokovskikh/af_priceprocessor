using System;
using System.Collections.Generic;
using System.Linq;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;
using NUnit.Framework;

namespace PriceProcessor.Test.Loader
{
	[TestFixture]
	public class XmlReaderFixture
	{
		[Test]
		public void Read_price()
		{
			var reader = new PriceXmlReader(@"..\..\Data\FarmaimpeksPrice.xml");
			Assert.That(reader.Prices().ToList().Count, Is.EqualTo(35));
		}

		[Test]
		public void Read_position()
		{
			var reader = new PriceXmlReader(@"..\..\Data\FarmaimpeksPrice.xml");
			reader.CostDescriptions = new List<CostDescription> {new CostDescription()};
			foreach (var price in reader.Prices())
			{
				var positions = reader.Read().ToList();
				Assert.That(positions.Count, Is.GreaterThan(0));

				var customers = reader.Customers().ToList();
				Assert.That(customers.Count, Is.GreaterThan(-1));
			}
		}
	}
}