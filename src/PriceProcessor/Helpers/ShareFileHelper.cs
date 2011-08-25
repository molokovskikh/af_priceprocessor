using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Inforoom.PriceProcessor.Helpers
{
	public class WaitFileException : Exception
	{
		public WaitFileException(string message)
			: base(message)
		{}
	}

	public class ShareFileHelper
	{
		public static void WaitFile(string fileName, int waitMs = 1000, int sleepMs = 100)
		{			
			DateTime startTime = DateTime.Now;
			while (!File.Exists(fileName))
			{
				if((DateTime.Now - startTime).TotalMilliseconds >= waitMs)
				{
					throw new WaitFileException(String.Format("Файл {0} не появился в папке после {1} мс ожидания.", fileName, waitMs));
				}
				Thread.Sleep(sleepMs);
			}
		}
	}
}
