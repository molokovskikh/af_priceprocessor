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
		private string[] _pricesPaths = new string[] { @"http://www.ru/rus", @"http://www.ru/rus/" };

		private string[] _pricesMasks = new string[] { @"index.about.html", @"index.about.html" };

		[Test]
		public void SourceHandlerTest()
		{
			var count = _pricesPaths.Length;
			var query = @"
update farm.sources
set sourcetypeid = 3
where sourcetypeid = 2
";
			With.Connection(connection => { MySqlHelper.ExecuteNonQuery(connection, query); });
			Setup.Initialize("DB");
			var priceItems = new TestPriceItem[count];
			for (var i = 0; i < count; i++)
				priceItems[i] = TestPriceSource.CreateHttpPriceSource(_pricesPaths[i], _pricesMasks[i], null);

			var handler = new HTTPSourceHandler();
			handler.StartWork();
			Thread.Sleep(5000);
			handler.StopWork();

			for (var i = 0; i < count; i++)
			{				
				var querySelectDownlogId = String.Format(@"
select count(RowId)
from `logs`.downlogs
where PriceItemId = {0}", priceItems[i].Id);
				var countDownlogId = 0;
				With.Connection(
					connection => { countDownlogId = Convert.ToInt32(MySqlHelper.ExecuteScalar(connection, querySelectDownlogId)); });
				Assert.That(countDownlogId, Is.EqualTo(1));
			}
		}
	}
}
