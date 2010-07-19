using System;
using Common.MySql;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Properties;
using LumiSoft.Net.FTP.Server;
using NUnit.Framework;
using Test.Support;
using System.Threading;
using System.IO;
using Inforoom.Downloader.Ftp;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace PriceProcessor.Test.Handlers
{
	[TestFixture]
	public class FTPSourceHandlerFixture
	{
		[Test]
		public void SubtractTotalMinutes()
		{
			var lastDateTime = DateTime.Now;
			var prevDateTime = DateTime.Now.AddHours(-3);
			Assert.That(lastDateTime.Subtract(prevDateTime).TotalMinutes > 0, "Получилось не положительное число");
			Assert.That(prevDateTime.Subtract(lastDateTime).TotalMinutes < 0, "Получилось не отрицательное число");
		}

		[Test]
		public void Download_file_from_ftp_server()
		{
			var ftpServer = new FTP_Server();
			ftpServer.StartServer();
			ftpServer.StopServer();
		}

		private TestPriceItem[] Process(string[] pricesPaths, string[] pricesMasks, string[] extrMasks)
		{
			return Process(pricesPaths, pricesMasks, extrMasks, null, null, null);
		}

		private TestPriceItem[] Process(string[] pricesPaths, string[] pricesMasks, string[] extrMasks, string ftpLogin, string ftpPassword, string ftpDir)
		{
			var count = pricesPaths.Length;
			var query = @"update farm.sources set sourcetypeid = 2 where sourcetypeid = 3";
			With.Connection(connection => { MySqlHelper.ExecuteNonQuery(connection, query); });
			var priceItems = new TestPriceItem[count];
			for (var i = 0; i < count; i++)
			{
				var pricePath = (pricesPaths.Length > 0) ? pricesPaths[i] : String.Empty;
				var priceMask = (pricesMasks.Length > 0) ? pricesMasks[i] : String.Empty;
				var extrMask = (priceMask.Length > 0) ? extrMasks[i] : String.Empty;
				priceItems[i] = TestPriceSource.CreateFtpPriceSource(pricePath, priceMask, extrMask, ftpLogin, ftpPassword, ftpDir);
				var sql = String.Format(@"delete from usersettings.priceitems where SourceId = {0} and Id <> {1}",
										priceItems[i].Source.Id, priceItems[i].Id);
				With.Connection(connection => { MySqlHelper.ExecuteNonQuery(connection, sql); });
			}
			var handler = new FTPSourceHandler();
			handler.StartWork();
			Thread.Sleep(6000);
			handler.StopWork();
			return priceItems;			
		}

		[Test(Description = "Тест попытки взаимодействия с ФТП при неправильном логине/пароле")]
		public void FtpInvalidLoginOrPassword()
		{
			var pricesPaths = new [] { "ftp.ahold.comch.ru" };
			var ftpLogin = "test";
			var ftpPassword = "123123123asd";
			var ftpDir = "tmp";
			var priceItems = Process(pricesPaths, pricesPaths, pricesPaths, ftpLogin, ftpPassword, ftpDir);
			var count = priceItems.Length;
			for (var i = 0; i < count; i++)
				CheckErrorMessage(priceItems[i], FtpSourceHandlerException.ErrorMessageInvalidLoginOrPassword);
		}

		[Test]
		public void FtpNetworkError()
		{
			var pricesPaths = new[] { "ftp.ru1" };
			var priceItems = Process(pricesPaths, pricesPaths, pricesPaths);
			for (var i = 0; i < priceItems.Length; i++)
				CheckErrorMessage(priceItems[i], FtpSourceHandlerException.NetworkErrorMessage);
		}

		private void CheckErrorMessage(TestPriceItem priceItem, string etalonMessage)
		{
			var query = String.Format(@"select ShortErrorMessage from `logs`.downlogs where PriceItemId = {0}", priceItem.Id);
			var message = String.Empty;
			With.Connection(connection => {
				message = MySqlHelper.ExecuteScalar(connection, query).ToString();
			});
			Assert.That(message.Contains(etalonMessage), Is.True);			
		}

		[Test]
		public void FtpChangePassiveModeTest()
		{
			var pricesPaths = new[] { "217.173.73.200" };
			var pricesMasks = new[] { "price.rar" };
			var extrMasks = new[] { "price*.dbf" };
			var ftpLogin = "inforum";
			var ftpPassword = "44Hr6FT3";
			var ftpDir = "price";

			TestHelper.RecreateDirectories();
			var priceItems = Process(pricesPaths, pricesMasks, extrMasks, ftpLogin, ftpPassword, ftpDir);

			var sql = String.Format(@"select count(*) from `logs`.downlogs where PriceItemId = {0}", priceItems[0].Id);
			var count = 0;
			With.Connection(connection => count = Convert.ToInt32(MySqlHelper.ExecuteScalar(connection, sql)));
			var files = Directory.GetFiles(Settings.Default.InboundPath);
			Assert.That(count, Is.EqualTo(1));
		}

		[Test]
		public void DownloadFile()
		{
			
		}
	}
}