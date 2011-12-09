using System;
using System.IO;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.Downloader;
using Inforoom.PriceProcessor;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using PriceSourceType = Test.Support.PriceSourceType;

namespace PriceProcessor.Test.Handlers
{
	[TestFixture]
	public class LanSourceHandlerFixture
	{
		[SetUp]
		public void Setup()
		{
			TestHelper.InitDirs(
				Settings.Default.FTPOptBoxPath,
				Settings.Default.InboundPath);
		}

		[Test, Ignore("Починить")]
		public void After_price_download_last_download_date_should_be_updated()
		{
			using (new TransactionScope())
			{
				TestPriceSource.Queryable
					.Where(s => s.SourceType == PriceSourceType.Lan)
					.ToList()
					.Each(s => s.Delete());
			}

			var begin = DateTime.Now;
			var source = TestPriceSource.CreateLanPriceSource("552.zip", "552.dbf");

			var dir = Path.Combine(Settings.Default.FTPOptBoxPath, "1179");
			if (Directory.Exists(dir))
				Directory.Delete(dir, true);
			Directory.CreateDirectory(dir);

			var ftpFile = Path.Combine(dir, "552.zip");
			File.Copy(@"..\..\Data\HandlersTests\552.zip", ftpFile);

			var handler = new LANSourceHandler();
			handler.StartWork();
			Thread.Sleep(10000);
			handler.StopWork();

			Assert.That(File.Exists(ftpFile), Is.False, "не удалили файл с ftp");
			using (new SessionScope())
			{
				var reloaded = TestPriceItem.Find(source.Id);
				Assert.That(reloaded.LastDownload, Is.EqualTo(new DateTime(2009, 12, 11, 10, 36, 0)));
				var logs = PriceDownloadLog.Queryable.Where(l => l.LogTime >= begin && l.PriceItemId == reloaded.Id).ToList();
				Assert.That(logs.Count, Is.EqualTo(1));
				var log = logs.First();
				Assert.That(log.PriceItemId, Is.EqualTo(reloaded.Id));
				Assert.That(log.ResultCode, Is.EqualTo(2));

				var inboundFile = Path.Combine(Settings.Default.InboundPath, String.Format("d{0}_{1}.dbf", reloaded.Id, log.Id));
				Console.WriteLine(inboundFile);
				Assert.That(File.Exists(inboundFile), Is.True, "не скопировали файл в inbound");
			}
		}
	}
}
