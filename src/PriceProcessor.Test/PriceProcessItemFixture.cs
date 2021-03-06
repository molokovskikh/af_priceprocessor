﻿using System.Collections.Generic;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class PriceProcessItemFixture
	{
		[Test]
		public void IsReadyForProcessing()
		{
			var item = new PriceProcessItem(false, 1, 1, 1, "", 1);
			var processing = new List<PriceProcessThread> { new PriceProcessThread(item, "") };
			Assert.That(item.IsReadyForProcessing(processing), Is.False);
		}

		[Test]
		public void Ignore_non_price_files()
		{
			var item = PriceProcessItem.TryToLoadPriceProcessItem("Thumbs.db");
			Assert.That(item, Is.Null);
		}

		[Test]
		public void Parse_price_item_id()
		{
			var id = PriceProcessItem.ParseId(".db");
			Assert.That(id, Is.EqualTo(0));
			id = PriceProcessItem.ParseId("1.db");
			Assert.That(id, Is.EqualTo(1));
			Assert.That(PriceProcessItem.ParseId("d1287_9972279.txt"), Is.EqualTo(1287));
		}
	}
}