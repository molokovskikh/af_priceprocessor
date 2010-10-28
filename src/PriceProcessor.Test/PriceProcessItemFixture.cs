using System.Collections.Generic;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class PriceProcessItemFixture
	{
		[Test]
		public void IsSynonymEqualTest()
		{
			Assert.That(new PriceProcessItem(false, 1, 1, 1, "", null)
				.IsSynonymEqual(new PriceProcessItem(false, 2, 2, 1, "", 1)), Is.True);

			Assert.That(new PriceProcessItem(false, 1, 1, 1, "", 4)
				.IsSynonymEqual(new PriceProcessItem(false, 2, 2, 2, "", 4)), Is.True);

			Assert.That(new PriceProcessItem(false, 1, 1, 1, "", 2)
				.IsSynonymEqual(new PriceProcessItem(false, 2, 2, 2, "", null)), Is.True);

			Assert.That(new PriceProcessItem(false, 1, 1, 1, "", null)
				.IsSynonymEqual(new PriceProcessItem(false, 2, 2, 2, "", null)), Is.False);

			//разные ценовые колонки одного прайса
			Assert.That(new PriceProcessItem(false, 1, 1, 1, "", null)
				.IsSynonymEqual(new PriceProcessItem(false, 1, 2, 2, "", null)), Is.True);

		}

		[Test]
		public void IsReadyForProcessing()
		{
			var item = new PriceProcessItem(false, 1, 1, 1, "", 1);
			var processing = new List<PriceProcessThread> { new PriceProcessThread(item, "") };
			Assert.That(item.IsReadyForProcessing(processing), Is.False);
		}
	}
}
