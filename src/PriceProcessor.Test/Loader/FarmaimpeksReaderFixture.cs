using System;
using System.Collections.Generic;
using System.Linq;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;
using NUnit.Framework;

namespace PriceProcessor.Test.Loader
{
	[TestFixture]
	public class FarmaimpeksReaderFixture
	{
		[Test]
		public void Read_price()
		{
			var reader = new FarmaimpeksReader(@"..\..\Data\FarmaimpeksPrice.xml");
			Assert.That(reader.Prices().Count(), Is.EqualTo(35));
		}

		[Test]
		public void Read_position()
		{
			var reader = new FarmaimpeksReader(@"..\..\Data\FarmaimpeksPrice.xml");
			reader.CostDescriptions = new List<CostDescription> { new CostDescription() };
			foreach (var price in reader.Prices()) {
				var positions = reader.Read().ToList();
				Assert.That(positions.Count, Is.GreaterThan(0));

				var customers = reader.Settings().ToList();
				Assert.That(customers.Count, Is.GreaterThan(-1));
			}
		}
	}
}