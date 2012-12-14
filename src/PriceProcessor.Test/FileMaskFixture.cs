using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class FileMaskFixture
	{
		[Test]
		public void simple_test()
		{
			Assert.IsTrue(WaybillService.FitsMask("123.txt", "*.txt"));
			Assert.IsTrue(WaybillService.FitsMask("123.txt", "123*"));
			Assert.IsFalse(WaybillService.FitsMask("123.txt", "123*."));
			Assert.IsTrue(WaybillService.FitsMask("123.txt", "123*.*"));
			Assert.IsFalse(WaybillService.FitsMask("123.txt", "*3.txy"));
			Assert.IsFalse(WaybillService.FitsMask("123.dbf", "*3.txt"));
			Assert.IsTrue(WaybillService.FitsMask("123.dbf", "*.d*"));
			Assert.IsFalse(WaybillService.FitsMask("123.dbf", "*.db"));
			Assert.IsTrue(WaybillService.FitsMask("123.dbf", "*.*"));
			Assert.IsFalse(WaybillService.FitsMask("123.dbf", "*.*g"));
			Assert.IsTrue(WaybillService.FitsMask("123.dbf", "*.*b*"));
			Assert.IsFalse(WaybillService.FitsMask("123.dbf", "*4.*"));
		}
	}
}
