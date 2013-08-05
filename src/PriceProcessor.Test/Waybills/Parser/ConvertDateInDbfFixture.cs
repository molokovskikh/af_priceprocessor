using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Parser;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class ConvertDateInDbfFixture
	{
		[Test]
		public void Parse()
		{
			object test2 = "02.03";
			Assert.That(FakeDbf.Convert(test2, typeof(String)), Is.EqualTo("02.03"));
			object test1 = "12/03/12";
			Assert.That(FakeDbf.Convert(test1, typeof(String)), Is.EqualTo("12.03.2012"));
			object test3 = "Text";
			Assert.That(FakeDbf.Convert(test3, typeof(String)), Is.EqualTo("Text"));
		}
	}

	public class FakeDbf : DbfParser
	{
		public static object Convert(object value, Type type)
		{
			return ConvertIfNeeded(value, type);
		}
	}
}
