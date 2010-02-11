using System;
using System.IO;
using Inforoom.PriceProcessor.Rosta;
using NUnit.Framework;

namespace PriceProcessor.Test.Special
{
	[TestFixture]
	public class RostaParserFixture
	{
		[Test]
		public void Read_extended_columns()
		{
			var data = RostaReader.ReadAddtions(@"..\..\Data\rosta\ex");
			var addition = data[0];
			Assert.That(addition.Period, Is.EqualTo(new DateTime(2013, 10, 07)));
			Assert.That(addition.Id, Is.EqualTo(8204));
			addition = data[1];
			Assert.That(addition.Period, Is.EqualTo(new DateTime(2011, 06, 22)));
		}

		[Test, Ignore("Для тестирования руками, часто не запускать что бы не спалиться")]
		public void Download_price()
		{
			if (Directory.Exists("output"))
				Directory.Delete("output", true);

			Directory.CreateDirectory("output");
			var downloader = new RostaDownloader();
			downloader.DownloadPrice("20100120154920-157-12", "price", "producers", "ex");
		}

	}
}
