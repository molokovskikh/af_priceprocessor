﻿using System;
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
			var w = WaybillParser.Parse(@"C:\сиаR-1733042плохой.txt");
		}
	}
}