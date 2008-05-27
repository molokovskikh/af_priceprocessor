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

		[Test]
		public void EqualsTest()
		{ 
			object Exists;
			object Current;
			Exists = DBNull.Value;
			Current = DBNull.Value;
			Assert.AreEqual(true, Current.Equals(Exists));
			Current = 1;
			Assert.AreEqual(false, Current.Equals(Exists));
			Current = "this is test";
			Assert.AreEqual(false, Current.Equals(Exists));
			Exists = 1.3;
			Current = 1;
			Assert.AreEqual(false, Current.Equals(Exists));
			Exists = "dsdsds";
			Current = 1;
			Assert.AreEqual(false, Current.Equals(Exists));
			Exists = (int)1;
			Current = (int)1;
			Assert.AreEqual(true, Current.Equals(Exists));
			Exists = "correct";
			Current = "correct";
			Assert.AreEqual(true, Current.Equals(Exists));
		}

		[Test]
		public void StringBuilderTest()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("this is test");
			sb.AppendLine("this is test 2");
			StringBuilder sbNew = new StringBuilder();
			sbNew.AppendFormat("this is test{0}\r\n", String.Empty);
			sbNew.AppendFormat("this is test 2{0}\r\n", String.Empty);
		}
	}
}
