using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Inforoom.Downloader;
using System.Threading;
using Test.Support;
using MySql.Data.MySqlClient;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class HTTPSourceHandlerFixture
	{
		public TestPriceItem[] SourceHandlerTest(string[] pricesPaths, string[] pricesMasks, string[] extrMasks)
		{
			var count = pricesPaths.Length;
			var query = @"update farm.sources set sourcetypeid = 3 where sourcetypeid = 2";
			With.Connection(connection => { MySqlHelper.ExecuteNonQuery(connection, query); });
			Setup.Initialize("DB");
			var priceItems = new TestPriceItem[count];
			for (var i = 0; i < count; i++)
			{
				var pricePath = (pricesPaths.Length > 0) ? pricesPaths[i] : String.Empty;
				var priceMask = (pricesMasks.Length > 0) ? pricesMasks[i] : String.Empty;
				var extrMask = (extrMasks.Length > 0) ? extrMasks[i] : String.Empty;
				priceItems[i] = TestPriceSource.CreateHttpPriceSource(pricePath, priceMask, extrMask);
			}
			var handler = new HTTPSourceHandler();
			handler.StartWork();
			Thread.Sleep(5000);
			handler.StopWork();

			return priceItems;
		}


		[Test]
		public void DownloadFileFromHttp()
		{
			var pricesPaths = new [] { @"http://www.ru/rus", @"http://www.ru/rus/" };
			var pricesMasks = new [] { @"index.about.html", @"index.about.html" };	
		
			var priceItems = SourceHandlerTest(pricesPaths, pricesMasks, new string[0]);
			CheckDownloadedFile(priceItems);
		}

		[Test]
		public void InvalidLoginOrPasswordTest()
		{
			var pricesPaths = new [] { @"http://www.orel.siaint.ru/analit" };

			var priceItems = SourceHandlerTest(pricesPaths, pricesPaths, new string[0]);

			foreach (var item in priceItems)
			{
				CheckShortErrorMessage(item, HttpSourceHandlerException.ErrorMessageUnauthorized);
			}
		}

		[Test]
		public void DownloadNetworkErrorTest()
		{
			var pricesPaths = new[] { @"http://www.ru1" };
			var priceItems = SourceHandlerTest(pricesPaths, pricesPaths, new string[0]);

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
