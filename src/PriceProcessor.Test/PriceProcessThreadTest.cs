using System;
using System.Collections.Generic;
using System.Text;
using Inforoom.PriceProcessor;
using Inforoom.Formalizer;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using System.Threading;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class PriceProcessThreadTest
	{
		[Test(Description = "тестирование методов AbortThread и IsAbortingLong")]
		public void AbortingThreadTest()
		{
			PriceProcessItem _priceProcessItem = new PriceProcessItem(false, 4596, null, 708, @"D:\Temp\Inbound0\708.dbf");
			PriceProcessThread _priceProcessThread = new PriceProcessThread(_priceProcessItem, String.Empty);
			while ((_priceProcessThread.ThreadState != ThreadState.Running) && _priceProcessThread.ThreadIsAlive)
			{
				Thread.Sleep(500);
			}
			_priceProcessThread.AbortThread();
			Assert.That(!_priceProcessThread.IsAbortingLong, "Ошибка в расчете времени прерывания");
			while (!_priceProcessThread.FormalizeEnd && (_priceProcessThread.ThreadState != ThreadState.Stopped))
				Thread.Sleep(500);
			Assert.That(!_priceProcessThread.IsAbortingLong, "Ошибка в расчете времени прерывания");
		}
	}
}
