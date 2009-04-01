using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class FTPSourceHandlerFixture
	{
		[Test]
		public void SubtractTotalMinutes()
		{
			DateTime lastDateTime = DateTime.Now;
			DateTime prevDateTime = DateTime.Now.AddHours(-3);
			Assert.That(lastDateTime.Subtract(prevDateTime).TotalMinutes > 0, "Получилось не положительное число");
			Assert.That(prevDateTime.Subtract(lastDateTime).TotalMinutes < 0, "Получилось не отрицательное число");		
		}
	}
}
