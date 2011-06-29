﻿using System;
using System.Threading;
using Common.MySql;
using NUnit.Framework;
using Inforoom.Downloader;
using Test.Support;
using System.Collections;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace PriceProcessor.Test.Handlers
{
	[TestFixture]
	public class PathSourceHandlerTest : HTTPSourceHandler
	{
		[SetUp]
		public void Setup()
		{
			TestHelper.RecreateDirectories();
			SourceType = "HTTP";
			TestPriceSource.CreateHttpPriceSource("www.ru", "index.html", "index.html");
			CreateDirectoryPath();
		}

		[Test, Ignore("Починить")]
		public void TestCheckDownloadInterval()
		{
			var source = new PriceSource {
				PriceDateTime = DateTime.Now,
				RequestInterval = 10
			};
			Assert.IsFalse(IsReadyForDownload(source));

			source.RequestInterval = 0;
			Assert.IsTrue(IsReadyForDownload(source));

			source.PriceDateTime = DateTime.Now.Subtract(new TimeSpan(0, 1, 0));
			source.RequestInterval = 10;
			Assert.IsTrue(IsReadyForDownload(source));
		}

		[Test]
		public void TestCheckDownloadInterval_IfFailed()
		{
			var source = new PriceSource {
				PriceDateTime = DateTime.Now,
				RequestInterval = 10,
				PriceItemId = 1,
			};

			FailedSources.Add(source.PriceItemId);
			Assert.IsTrue(IsReadyForDownload(source));
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

		[Test, Ignore("Починить")]
		public void TestDownloadSeveralFiles()
		{
			var sql = @"
delete from `logs`.downlogs where LogTime > curdate();
update farm.sources set sourcetypeid = 3 where sourcetypeid = 2;";
			With.Connection(connection => { MySqlHelper.ExecuteNonQuery(connection, sql); });
			TestHelper.RecreateDirectories();
			var source = TestPriceSource.CreateHttpPriceSource("www.ru/rus", "index.pressa.html", "index.pressa.html");
			source.LastDownload = new DateTime(DateTime.Now.Year, 1, 1);
			source.SaveAndFlush();
			source = TestPriceSource.CreateHttpPriceSource("www.ru/rus", "index.html", "index.html");
			source.LastDownload = DateTime.Now.AddDays(30);
			source.SaveAndFlush();
			source = TestPriceSource.CreateHttpPriceSource("www.ru/rus", "index.about.html", "index.about.html");
			source.LastDownload = new DateTime(DateTime.Now.Year, 1, 1);
			source.SaveAndFlush();
			var handler = new HTTPSourceHandler();
			handler.StartWork();
			Thread.Sleep(6000);
			handler.StopWork();
			sql = @"select count(*) from `logs`.downlogs where LogTime > curdate();";
			var count = 0;
			With.Connection(connection => { count = Convert.ToInt32(MySqlHelper.ExecuteScalar(connection, sql)); });
			Assert.That(count, Is.EqualTo(2));
		}
	}
}
