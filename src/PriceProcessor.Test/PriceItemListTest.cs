using System;
using System.Collections.Generic;
using System.Text;
using Inforoom.PriceProcessor;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

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
			Assert.AreEqual(c, b, "Последний добавленный прайс-лист выбран некорреткно.");
		}

		[Test]
		public void GetDownloadedListCountTest()
		{
			Assert.AreEqual(0, PriceItemList.GetDownloadedCount(), "Кол-во загруженных прайс-листов не равно нулю.");
		}

		[Test]
		public void FindAllInListTest()
		{
			PriceProcessItem item1 = new PriceProcessItem(true, 1, 1, 1, "test1.txt");
			item1.FileTime = DateTime.Now;
			PriceProcessItem item2 = new PriceProcessItem(true, 2, 2, 2, "test2.txt");
			item2.FileTime = DateTime.Now;
			PriceProcessItem item3 = new PriceProcessItem(true, 3, 3, 3, "test3.txt");
			item3.FileTime = DateTime.Now;
			PriceItemList.AddItem(item1);
			PriceItemList.AddItem(new PriceProcessItem(false, 4, 4, 4, "test4.txt"));
			PriceItemList.AddItem(item2);
			PriceItemList.AddItem(new PriceProcessItem(false, 5, 5, 5, "test5.txt"));
			PriceItemList.AddItem(item3);
			PriceItemList.AddItem(new PriceProcessItem(false, 6, 6, 6, "test6.txt"));
			List<PriceProcessItem> findAllDownloadedList = PriceItemList.GetDownloadedItemList();
			Assert.AreEqual(item1, findAllDownloadedList[0], "Элемент с индексом 0 выбран некорректно.");
			Assert.AreEqual(item2, findAllDownloadedList[1], "Элемент с индексом 1 выбран некорректно.");
			Assert.AreEqual(item3, findAllDownloadedList[2], "Элемент с индексом 2 выбран некорректно.");
		}

		[Test]
		public void DeleteFormChildListTest()
		{
			PriceProcessItem item1 = new PriceProcessItem(true, 1, 1, 1, "test1.txt");
			item1.FileTime = DateTime.Now;
			PriceProcessItem item2 = new PriceProcessItem(true, 2, 2, 2, "test2.txt");
			item2.FileTime = DateTime.Now;
			PriceItemList.AddItem(item1);
			PriceItemList.AddItem(new PriceProcessItem(false, 4, 4, 4, "test4.txt"));
			PriceItemList.AddItem(item2);
			List<PriceProcessItem> findAllDownloadedList = PriceItemList.GetDownloadedItemList();
			findAllDownloadedList.Remove(item1);
			Assert.AreEqual(true, PriceItemList.DownloadedExists(item1.PriceItemId), "Элемент не найден в списке.");
			PriceItemList.list.Remove(item1);
			Assert.AreEqual(false, PriceItemList.DownloadedExists(item1.PriceItemId), "Элемент найден в списке.");
		}

		[Test]
		public void CompareListsTest()
		{
			PriceProcessItem item1 = new PriceProcessItem(true, 1, 1, 1, "test1.txt");
			item1.FileTime = DateTime.Now;
			PriceProcessItem item2 = new PriceProcessItem(true, 2, 2, 2, "test2.txt");
			item2.FileTime = DateTime.Now;
			PriceItemList.AddItem(item1);
			PriceItemList.AddItem(new PriceProcessItem(false, 4, 4, 4, "test4.txt"));
			PriceItemList.AddItem(item2);
			List<PriceProcessItem> findAllDownloadedList = PriceItemList.GetDownloadedItemList();
			Assert.AreEqual(true, findAllDownloadedList != PriceItemList.list, "Список загруженных прайс-листов равен списку всех прайс-листов");
			findAllDownloadedList = PriceItemList.list;
			Assert.AreEqual(false, findAllDownloadedList != PriceItemList.list, "Списки не равны");
			PriceItemList.list.RemoveAt(1);
			findAllDownloadedList = PriceItemList.GetDownloadedItemList();
			Assert.AreEqual(true, findAllDownloadedList != PriceItemList.list, "Список загруженных прайс-листов равен списку всех прайс-листов");
		}

	}
}
