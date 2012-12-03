using System;
using System.Collections.Generic;
using System.Text;
using Inforoom.PriceProcessor;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class PriceItemListTest
	{
		[SetUp]
		public void Init()
		{
			PriceItemList.list.Clear();
		}

		[Test]
		public void Priritize_by_downloaded()
		{
			PriceItemList.list.Add(new PriceProcessItem(false, 1, 1, 1, "", null));
			PriceItemList.list.Add(new PriceProcessItem(true, 1, 1, 1, "", null) { CreateTime = new DateTime(2012, 12, 3, 9, 10, 0) });
			PriceItemList.list.Add(new PriceProcessItem(true, 1, 1, 1, "", null) { CreateTime = new DateTime(2012, 12, 3, 9, 00, 0) });
			var priceProcessItems = PriceItemList.GetPrioritizedList();
			Assert.That(priceProcessItems[0].Downloaded, Is.True);
			Assert.That(priceProcessItems[0].CreateTime, Is.EqualTo(new DateTime(2012, 12, 3, 9, 0, 0)));
		}

		[Test]
		public void SortListTest()
		{
			var a = new PriceProcessItem(true, 1, 1, 1, "test.txt", null);
			a.FileTime = DateTime.Now.AddHours(-1);
			PriceItemList.AddItem(a);
			var b = new PriceProcessItem(true, 1, 1, 1, "test.txt", null);
			b.FileTime = DateTime.Now;
			PriceItemList.AddItem(b);
			PriceProcessItem c = PriceItemList.GetLastestDownloaded(1);
			Assert.AreEqual(c, b, "Последний добавленный прайс-лист выбран некорреткно.");
		}
	}
}