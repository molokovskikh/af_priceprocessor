using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Inforoom.Downloader;
using Test.Support;
using System.Collections;

namespace PriceProcessor.Test.Handlers
{
	[TestFixture]
	public class PathSourceHandlerTest : HTTPSourceHandler
	{
		[SetUp]
		public void Setup()
		{
			sourceType = "HTTP";
			TestPriceSource.CreateHttpPriceSource("www.ru", "index.html", "index.html");
			CreateDirectoryPath();
			CreateWorkConnection();			
		}

		protected override void GetFileFromSource(PriceSource row)
		{
			throw new NotImplementedException();
		}

		[Test]
		public void TestCheckDownloadInterval()
		{
			var source = new FakePriceSource {
				PriceDateTime = DateTime.Now,
				Interval = 10
			};
			Assert.IsFalse(CheckDownloadInterval(source));

			source.Interval = 0;
			Assert.IsTrue(CheckDownloadInterval(source));

			source.PriceDateTime = DateTime.Now.Subtract(new TimeSpan(0, 1, 0));
			source.Interval = 10;
			Assert.IsTrue(CheckDownloadInterval(source));
		}

		[Test]
		public void TestCheckDownloadInterval_IfFailed()
		{
			var source = new FakePriceSource {
				PriceDateTime = DateTime.Now,
				Interval = 10,
				PriceItemId = 1,
			};

			FailedSources.Add(source.PriceItemId);
			Assert.IsTrue(CheckDownloadInterval(source));
		}
		
		[Test]
		public void TestAddFailedSourceToList()
		{
			FailedSources.Clear();
			FillSourcesTable();
			
			var listSources = new ArrayList();
			while (dtSources.Rows.Count > 0)
			{
				var priceSource = new PriceSource(dtSources.Rows[0]);
				var likeRows = GetLikeSources(priceSource);
				foreach (var likeRow in likeRows)
					likeRow.Delete();
				dtSources.AcceptChanges();				
				listSources.Add(priceSource);
			}
			ProcessData();

			foreach (PriceSource item in listSources)
				Assert.IsTrue(FailedSources.Contains(item.PriceItemId));			
		}
	}

	public class FakePriceSource : PriceSource
	{
		public int Interval;

		public override int RequestInterval
		{
			get { return Interval; }
		}

		public override DateTime PriceDateTime { get; set; }
	}
}
