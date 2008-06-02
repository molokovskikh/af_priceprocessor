using System;
using System.Collections.Generic;
using System.Text;
using Inforoom.PriceProcessor;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;


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

		[Test]
		public void ListAddRangeTest()
		{
			List<string> all = new List<string>();
			all.Add("1");
			all.Add("2");
			all.Add("3");
			List<string> added = new List<string>();
			added.Add("6");
			added.Add("7");
			added.Add("8");
			all.InsertRange(0, added);
			List<string> compare = new List<string>();
			compare.Add("6");
			compare.Add("7");
			compare.Add("8");
			compare.Add("1");
			compare.Add("2");
			compare.Add("3");

			Assert.That(all, Is.EquivalentTo(compare));
		}
	}
}
