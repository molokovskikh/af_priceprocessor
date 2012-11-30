using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Models;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;
using PriceSourceType = Test.Support.PriceSourceType;

namespace PriceProcessor.Test.Handlers
{
	public class FakeHandler : HTTPSourceHandler
	{
		public FakeHandler()
		{
			SourceType = "HTTP";
		}

		public void LogDownloadedPriceTest(ulong sourceTypeId, string archFileName, string extrFileName)
		{
			LogDownloadedPrice(sourceTypeId, archFileName, extrFileName);
		}

		public override void ProcessData()
		{
			throw new NotImplementedException();
		}

		protected override void CopyToHistory(ulong downloadLogId)
		{ }
		public void SetDrCurrent(int rowCount)
		{
			drCurrent = dtSources.Rows[rowCount];
			CurrPriceItemId = Convert.ToUInt64(drCurrent[0]);
		}

		public void SetDrCurrent(uint itemId)
		{
			drCurrent = dtSources.Select(String.Format("PriceItemId = {0}", itemId))[0];
			CurrPriceItemId = Convert.ToUInt64(drCurrent[0]);
		}
	}

	[TestFixture]
	public class BasePriceSourceHandlerFixture : IntegrationFixture
	{
		[Test]
		public void PriceLogTimeTest()
		{
			var supplier = TestSupplier.Create();
			var price = supplier.Prices[0];
			price.Costs[0].PriceItem.Format.PriceFormat = PriceFormatType.NativeDbf;
			session.Save(price.Costs[0].PriceItem.Format);
			price.Costs[0].PriceItem.Source.PriceMask = "*.dbf";
			price.Costs[0].PriceItem.Source.SourceType = PriceSourceType.Http;
			session.Save(price.Costs[0].PriceItem.Source);
			session.Save(price);
			Reopen();
			var file = File.Create("test1.dbf");
			file.Close();
			Thread.Sleep(1000);
			var handler = new FakeHandler();
			handler.FillSourcesTable();
			handler.SetDrCurrent(price.Costs[0].PriceItem.Id);
			var now = DateTime.Now;
			Thread.Sleep(1000);
			handler.LogDownloadedPriceTest(3, "test.dbf", "test1.dbf");
			var downlog = session.CreateSQLQuery(String.Format(@"select Max(logtime) from
logs.downlogs where PriceItemId = {0}",
				price.Costs[0].PriceItem.Id))
				.UniqueResult<DateTime>();
			var item = session.Load<PriceItem>(price.Costs[0].PriceItem.Id);
			Assert.That(item.LastDownload, Is.LessThanOrEqualTo(now));
			Assert.That(item.LastDownloadDate, Is.GreaterThanOrEqualTo(now));
			Assert.That(downlog, Is.GreaterThan(now));
		}
	}
}
