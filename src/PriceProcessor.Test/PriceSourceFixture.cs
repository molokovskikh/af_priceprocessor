using System;
using Common.Tools;
using Inforoom.Downloader;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class PriceSourceFixture
	{
		private PriceSource source;

		[SetUp]
		public void SetUp()
		{
			source = new PriceSource {
				RequestInterval = 180,
				LastSuccessfulCheck = new DateTime(2011, 1, 14, 17, 55, 30)
			};
		}

		[Test]
		public void Check_for_price_if_interval_expired()
		{
			SystemTime.Now = () => new DateTime(2011, 1, 14, 17, 58, 30);
			Assert.That(source.IsReadyForDownload(), Is.True);
		}

		[Test]
		public void Do_not_check_if_interval_not_expired()
		{
			SystemTime.Now = () => new DateTime(2011, 1, 14, 17, 56, 30);
			Assert.That(source.IsReadyForDownload(), Is.False);
		}

		[Test]
		public void Download_if_saved_date_greater_now()
		{
			source.LastSuccessfulCheck = new DateTime(2011, 1, 15, 17, 58, 30);
			SystemTime.Now = () => new DateTime(2011, 1, 14, 17, 58, 30);
			Assert.That(source.IsReadyForDownload(), Is.True);
		}

		[Test]
		public void Update_last_check()
		{
			source.UpdateLastCheck();
		}
	}
}