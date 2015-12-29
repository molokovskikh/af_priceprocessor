using System;
using System.Threading;
using Castle.ActiveRecord;
using Common.MySql;
using NUnit.Framework;
using Inforoom.Downloader;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using System.Collections;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;
using PriceSourceType = Test.Support.PriceSourceType;

namespace PriceProcessor.Test.Handlers
{
	[TestFixture]
	public class HttpSourceHandlerFixture : BaseHandlerFixture<HTTPSourceHandler>
	{
		[SetUp]
		public void Setup()
		{
			source.SourceType = PriceSourceType.Http;
			source.Save();
		}

		[Test]
		public void TestCheckDownloadInterval()
		{
			var source = new PriceSource {
				PriceDateTime = DateTime.Now,
				RequestInterval = 10
			};
			source.UpdateLastCheck();
			Assert.IsFalse(handler.IsReadyForDownload(source));
			source.RequestInterval = 0;
			Assert.IsTrue(handler.IsReadyForDownload(source));
			Thread.Sleep(5000);
			source.PriceDateTime = DateTime.Now.Subtract(new TimeSpan(0, 1, 0));
			source.RequestInterval = 4;
			Assert.IsTrue(handler.IsReadyForDownload(source));
		}

		[Test]
		public void TestCheckDownloadInterval_IfFailed()
		{
			var source = new PriceSource {
				PriceDateTime = DateTime.Now,
				RequestInterval = 10,
				PriceItemId = 1,
			};

			handler.FailedSources.Add(source.PriceItemId);
			Assert.IsTrue(handler.IsReadyForDownload(source));
		}

		[Test, Ignore("Починить")]
		public void DownloadFileFromHttp()
		{
			source.PricePath = @"http://www.ru/rus";
			source.PriceMask = @"index.html";
			source.Save();
			Process();
			CheckDownloadedFile();

			source.PricePath = @"http://www.ru/rus/";
			source.PriceMask = @"index.about.html";
			source.Save();
			Process();
			CheckDownloadedFile();
		}

		[Test, Ignore("Починить")]
		public void InvalidLoginOrPasswordTest()
		{
			source.PricePath = @"https://stat.analit.net/ci/auth/logon.aspx";
			Process();

			CheckErrorMessage(priceItem, HttpSourceHandlerException.ErrorMessageUnauthorized);
		}

		[Test, Ignore("Починить")]
		public void DownloadNetworkErrorTest()
		{
			source.PricePath = @"http://www.ru1";
			Process();

			CheckErrorMessage(priceItem, PathSourceHandlerException.NetworkErrorMessage);
		}

		protected void CheckDownloadedFile()
		{
			var querySelectDownlogId = String.Format(@"select count(RowId) from `logs`.downlogs where PriceItemId = {0}", priceItem.Id);
			var countDownlogId = 0;
			With.Connection(connection => { countDownlogId = Convert.ToInt32(MySqlHelper.ExecuteScalar(connection, querySelectDownlogId)); });
			Assert.That(countDownlogId, Is.EqualTo(1));
		}
	}
}