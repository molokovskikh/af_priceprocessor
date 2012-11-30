using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.Downloader;
using NUnit.Framework;

namespace PriceProcessor.Test.Handlers
{
	public class FakeHandler : BasePriceSourceHandler
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
	}

	[TestFixture]
	public class BasePriceSourceHandlerFixture
	{
		[Test]
		public void SyncLogTimeTest()
		{
			var file = File.Create("test1.dbf");
			file.Close();
			var handler = new FakeHandler();
			handler.FillSourcesTable();
			handler.SetDrCurrent(0);
			handler.LogDownloadedPriceTest(3, "test.dbf", "test1.dbf");
		}
	}
}
