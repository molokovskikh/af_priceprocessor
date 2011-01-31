using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;
using NUnit.Framework;

namespace PriceProcessor.Test.Formalization
{
	[TestFixture]
	public class ProducerResolverFixture
	{
		[Test]
		public void ResolveProducerTest()
		{
			var file = @"..\..\Data\688-wrong-column-for-producers_brand.txt";
			var priceItemId = 700;

			var rules = new DataTable();
			rules.ReadXml(String.Format(@"..\..\Data\688-assortment-rules.xml", priceItemId));

			TestHelper.Formalize(typeof(DelimiterNativeTextParser1251), rules, file, priceItemId);
		}
		[Test]
		public void Test()
		{
			var reader = File.ReadAllLines(@"C:\test.txt");
			foreach (var s in reader)
			{
				Console.WriteLine(s);
			}
		}
	}
}
