using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using log4net;
using log4net.Config;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class LoggerTest
	{
		[Test]
		public void DBLoggerTest()
		{
			ILog logger = LogManager.GetLogger(GetType());
			XmlConfigurator.Configure();
			logger.Error("Alarm!!!");
		}
	}
}
