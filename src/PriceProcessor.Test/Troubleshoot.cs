using System;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test
{
	[TestFixture, Ignore("Тест что бы разбирать проблемные ситуации")]
	public class Troubleshoot
	{
		[Test]
		public void p()
		{
			var d = WaybillParser.Parse(@"C:\real №УТKП0005371 from 14.11.2013.dbf");
			Console.WriteLine(d.Parser);
		}
	}
}