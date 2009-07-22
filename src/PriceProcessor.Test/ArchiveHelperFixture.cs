using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Inforoom.Common;
using System.Configuration;
using System.Diagnostics;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class ArchiveHelperFixture
	{
		[Test]
		public void TestUseLocation()
		{
			try
			{
				ArchiveHelper.IsArchive(Environment.CurrentDirectory + "\\Data\\552.dbf");
			}
			catch (Exception exception)
			{
				Assert.Fail("получили неожидаемое исключение при активации ArchiveHelper: {0}", exception);
			}
		}
	}
}
