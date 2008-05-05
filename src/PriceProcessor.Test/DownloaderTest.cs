using System;
using System.Collections.Generic;
using System.Text;
using Inforoom.PriceProcessor;
using NUnit.Framework;


namespace PriceProcessor.Test
{
	[TestFixture]
	public class DownloaderTest
	{
		[Test]
		public void SortedListTest()
		{
			SortedList<DateTime, string> sl = new SortedList<DateTime, string>();
			sl.Add(new DateTime(2008, 01, 01), "младшая");
			sl.Add(new DateTime(2007, 01, 01), "старшая");
			Assert.AreEqual("старшая", sl.Values[0], "Сортировка некорректна");
		}
	}
}
