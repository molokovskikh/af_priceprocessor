using System;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test
{
	[TestFixture, Ignore("Тест что бы разбирать проблемные ситуации")]
	public class Troubleshoot
	{
		[Test]
		public void t()
		{
			var w = WaybillParser.Parse(@"C:\ПрофитмедСПб_791_434_14.dbf");
			Console.WriteLine(w.Parser);
		}
	}
}