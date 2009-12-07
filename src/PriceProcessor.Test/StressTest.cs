using System;
using System.Threading;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Properties;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture, Ignore("Стресс тест")]
	public class StressTest
	{
		[Test]
		public void Formilize_test()
		{
			BasicConfigurator.Configure(new ConsoleAppender(new SimpleLayout()));

			Settings.Default.SyncPriceCodes.Add("3779");
			Settings.Default.SyncPriceCodes.Add("2819");

			Program.InitDirs(new[] {
				Settings.Default.BasePath,
				Settings.Default.ErrorFilesPath,
				Settings.Default.InboundPath,
				Settings.Default.TempPath,
				Settings.Default.HistoryPath
			});

			var handler = new FormalizeHandler();
			handler.StartWork();
			Thread.Sleep(TimeSpan.FromMinutes(10000));
		}
	}
}
