using System;
using System.Data;
using System.Linq;
using Common.Tools;
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
			Console.WriteLine(Dbf.Load(@"C:\Users\kvasov\Downloads\12282-1_2401382.dbf").Columns.Cast<DataColumn>().OrderBy(x => x.ColumnName)
				.Implode(x => x.ColumnName, "\r\n"));
		}
	}
}
