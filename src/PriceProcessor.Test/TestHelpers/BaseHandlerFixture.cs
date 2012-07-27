using System;
using Common.MySql;
using Inforoom.Downloader;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;
using PriceSourceType = Test.Support.PriceSourceType;

namespace PriceProcessor.Test.TestHelpers
{
	public class BaseHandlerFixture<T>
		where T : BaseSourceHandler, new()
	{
		protected TestPriceItem priceItem;
		protected TestPriceSource source;
		protected TestSupplier supplier;
		protected T handler;

		[SetUp]
		public void Setup()
		{
			TestHelper.RecreateDirectories();

			supplier = TestSupplier.Create();
			priceItem = supplier.Prices[0].Costs[0].PriceItem;
			source = priceItem.Source;
			source.SourceType = PriceSourceType.Http;
			source.PricePath = "www.ru";
			source.PriceMask = "index.html";
			source.ExtrMask = "index.html";
			priceItem.Format.PriceFormat = PriceFormatType.NativeDbf;
			priceItem.Format.Save();

			handler = new T();

			handler.CreateDirectoryPath();
		}

		protected void CheckErrorMessage(TestPriceItem priceItem, string etalonMessage)
		{
			var query = String.Format(@"select ShortErrorMessage from `logs`.downlogs where PriceItemId = {0}", priceItem.Id);
			var message = String.Empty;
			With.Connection(connection => { message = MySqlHelper.ExecuteScalar(connection, query).ToString(); });
			Assert.That(message.Contains(etalonMessage), Is.True);
		}

		protected void Process()
		{
			source.Save();

			//var query = @"update farm.sources set sourcetypeid = 2 where sourcetypeid = 3";
			//With.Connection(connection => { MySqlHelper.ExecuteNonQuery(connection, query); });
			handler.ProcessData();
		}
	}
}