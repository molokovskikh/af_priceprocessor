using System;
using System.Threading;
using Castle.ActiveRecord;
using Common.MySql;
using Inforoom.Downloader;
using NUnit.Framework;
using Test.Support;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace PriceProcessor.Test.Handlers
{
	[TestFixture]
	public class HttpSourceHandlerFixture
	{
		public TestPriceItem[] Process(string[] pricesPaths, string[] pricesMasks, string[] extrMasks)
		{
			var count = pricesPaths.Length;
			var query = @"update farm.sources set sourcetypeid = 3 where sourcetypeid = 2";
			With.Connection(connection => { MySqlHelper.ExecuteNonQuery(connection, query); });
			var priceItems = new TestPriceItem[count];
			for (var i = 0; i < count; i++)
			{
				var pricePath = (pricesPaths.Length > 0) ? pricesPaths[i] : String.Empty;
				var priceMask = (pricesMasks.Length > 0) ? pricesMasks[i] : String.Empty;
				var extrMask = (extrMasks.Length > 0) ? extrMasks[i] : String.Empty;
				using(new SessionScope())
					priceItems[i] = TestPriceSource.CreateHttpPriceSource(pricePath, priceMask, extrMask);
				var sql = String.Format(@"delete from usersettings.priceitems where SourceId = {0} and Id <> {1}",
				                        priceItems[i].Source.Id, priceItems[i].Id);
				With.Connection(connection => { MySqlHelper.ExecuteNonQuery(connection, sql); });
				sql = String.Format(@"update farm.sources set RequestInterval = {0} where Id = {1}", 0, priceItems[i].Source.Id);
				With.Connection(connection => { MySqlHelper.ExecuteNonQuery(connection, sql); });
			}
			var handler = new HTTPSourceHandler();
			handler.StartWork();
			Thread.Sleep(7000);
			handler.StopWork();

			return priceItems;
		}

		[Test]
		public void DownloadFileFromHttp()
		{
			var pricesPaths = new[] { @"http://www.ru/rus", @"http://www.ru/rus/" };
			var pricesMasks = new[] { @"index.html", @"index.about.html" };

			var priceItems = Process(pricesPaths, pricesMasks, new string[0]);
			CheckDownloadedFile(priceItems);
		}

		[Test]
		public void InvalidLoginOrPasswordTest()
		{
			var pricesPaths = new[] { @"https://stat.analit.net/ci/auth/logon.aspx" };

			var priceItems = Process(pricesPaths, pricesPaths, pricesPaths);

			foreach (var item in priceItems)
			{
				CheckShortErrorMessage(item, HttpSourceHandlerException.ErrorMessageUnauthorized);
			}
		}

		[Test]
		public void DownloadNetworkErrorTest()
		{
			var pricesPaths = new[] { @"http://www.ru1" };
			var priceItems = Process(pricesPaths, pricesPaths, pricesPaths);

			foreach (var item in priceItems)
			{
				CheckShortErrorMessage(item, PathSourceHandlerException.NetworkErrorMessage);
			}
		}

		private void CheckShortErrorMessage(TestPriceItem priceItem, string etalonMessage)
		{
			var query = String.Format(@"select ShortErrorMessage from `logs`.downlogs where PriceItemId = {0}", priceItem.Id);
			var message = String.Empty;
			With.Connection(connection => { message = MySqlHelper.ExecuteScalar(connection, query).ToString(); });
			Assert.That(message.Contains(etalonMessage), Is.True);
		}

		private void CheckDownloadedFile(TestPriceItem[] priceItems)
		{
			for (var i = 0; i < priceItems.Length; i++)
			{
				var querySelectDownlogId = String.Format(@"select count(RowId) from `logs`.downlogs where PriceItemId = {0}", priceItems[i].Id);
				var countDownlogId = 0;
				With.Connection(connection => { countDownlogId = Convert.ToInt32(MySqlHelper.ExecuteScalar(connection, querySelectDownlogId)); });
				Assert.That(countDownlogId, Is.EqualTo(1));
			}
		}
	}
}
