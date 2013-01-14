using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Tools;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Formalizer;
using NUnit.Framework;

namespace PriceProcessor.Test.Handlers
{
	public class TestFormalizeHandler : FormalizeHandler
	{
		public TestFormalizeHandler()
		{
			_errorMessages = new Hashtable();
		}

		public List<PriceProcessThread> Threads
		{
			get { return pt; }
		}

		public void Process()
		{
			ProcessData();
		}
	}

	[TestFixture]
	public class FormalizationHandlerFixture
	{
		private TestFormalizeHandler handler;

		[SetUp]
		public void Setup()
		{
			Settings.Default.MaxWorkThread = 3;
			PriceItemList.list.Clear();
			handler = new TestFormalizeHandler();
		}

		[TearDown]
		public void TearDown()
		{
			handler.Threads.Each(t => t.AbortThread());
		}

		[Test]
		public void Do_not_put_more_than_one_price_with_same_setting_to_formalization()
		{
			var item = new PriceProcessItem(false, 1, 1, 1, "1.txt", null);
			item.CreateTime = item.CreateTime.AddMinutes(-10);
			File.WriteAllText("1.txt", "");
			PriceItemList.list.Add(item);

			item = new PriceProcessItem(false, 3, 3, 3, "2.txt", null);
			item.CreateTime = item.CreateTime.AddMinutes(-10);
			File.WriteAllText("3.txt", "");
			PriceItemList.list.Add(item);

			item = new PriceProcessItem(false, 2, 2, 2, "2.txt", 1);
			item.CreateTime = item.CreateTime.AddMinutes(-10);
			File.WriteAllText("2.txt", "");
			PriceItemList.list.Add(item);


			handler.Process();
			Assert.That(handler.Threads.Count, Is.EqualTo(2));
			Assert.That(handler.Threads[0].ProcessItem.PriceCode, Is.EqualTo(1));
			Assert.That(handler.Threads[1].ProcessItem.PriceCode, Is.EqualTo(3));
		}

		[Test]
		public void Top_In_Inbound_List_Test()
		{
			var dtn = DateTime.Now;
			var checkedItem = new PriceProcessItem(false, 5, null, 1, "jjj.AAA", null) { CreateTime = dtn.AddMinutes(50) };
			PriceItemList.AddItem(new PriceProcessItem(false, 1, null, 1, "jjj.123", null) { CreateTime = dtn.AddMinutes(10) });
			PriceItemList.AddItem(checkedItem);
			PriceItemList.AddItem(new PriceProcessItem(true, 2, null, 1, "jjj.345", null) { CreateTime = dtn.AddMinutes(100) });
			PriceItemList.AddItem(new PriceProcessItem(true, 3, null, 1, "jjj.789", null) { CreateTime = dtn.AddMinutes(100) });
			Assert.AreEqual(checkedItem.CreateTime, dtn.AddMinutes(50));
			var wcf = new WCFPriceProcessorService();
			wcf.TopInInboundList(checkedItem.GetHashCode());
			Assert.AreEqual(checkedItem.CreateTime, dtn.AddMinutes(10).AddSeconds(-5));
		}

		[Test]
		public void Delete_ItemIn_Inbound_List_Test()
		{
			var checkedItem = new PriceProcessItem(false, 5, null, 1, "jjj.AAA", null);
			PriceItemList.AddItem(new PriceProcessItem(false, 1, null, 1, "jjj.123", null));
			PriceItemList.AddItem(checkedItem);
			PriceItemList.AddItem(new PriceProcessItem(true, 2, null, 1, "jjj.345", null));
			PriceItemList.AddItem(new PriceProcessItem(true, 3, null, 1, "jjj.789", null));
			Assert.IsTrue(PriceItemList.list.Any(l => l.FilePath == "jjj.AAA"));
			var wcf = new WCFPriceProcessorService();
			wcf.DeleteItemInInboundList(checkedItem.GetHashCode());
			Assert.IsFalse(PriceItemList.list.Any(l => l.FilePath == "jjj.AAA"));
		}
	}
}