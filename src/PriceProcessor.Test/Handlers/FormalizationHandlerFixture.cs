using System.Collections;
using System.Collections.Generic;
using System.IO;
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
	}
}