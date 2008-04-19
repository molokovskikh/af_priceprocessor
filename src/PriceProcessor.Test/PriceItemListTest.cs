using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class PriceItemListTest
	{
		[Test]
		public void SortListTest()
		{
			PriceProcessItem a = new PriceProcessItem(true, 1, 1, 1, "test.txt");
			a.FileTime = DateTime.Now.AddHours(-1);
			PriceItemList.AddItem(a);
			PriceProcessItem b = new PriceProcessItem(true, 1, 1, 1, "test.txt");
			b.FileTime = DateTime.Now;
			PriceItemList.AddItem(b);
			PriceProcessItem c = PriceItemList.GetLastestDownloaded(1);
			//Assert.AreEqual(double.PositiveInfinity, double.PositiveInfinity);
			Assert.AreEqual(c, b, "Последний добавленный прайс-лист выбран некорреткно.");
		}
	}
}
