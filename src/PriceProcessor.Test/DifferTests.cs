using System;
using System.Collections.Generic;
using System.Linq;
using Common.Tools;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class DifferTests
	{
		[Test]
		public void RemoveDoubleSpacesTest()
		{
			IList<string> ls = new List<string>();
			ls.Add(" aaa         bbbb ccc       ddd ");
			ls.Add(String.Empty);
			ls.Add(null);
			  
			ls = ls.Select(l => l.RemoveDoubleSpaces()).ToList();

			Assert.That(ls[0], Is.EqualTo(" aaa bbbb ccc ddd "));
			Assert.That(ls[1], Is.EqualTo(String.Empty));
			Assert.That(ls[2], Is.EqualTo(String.Empty));
		}
	}
}
