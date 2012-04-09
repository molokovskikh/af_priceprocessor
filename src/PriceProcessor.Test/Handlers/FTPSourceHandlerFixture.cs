using System;
using Common.MySql;
using Inforoom.PriceProcessor;
using LumiSoft.Net.FTP.Server;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using System.IO;
using Inforoom.Downloader.Ftp;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace PriceProcessor.Test.Handlers
{
	[TestFixture]
	public class FtpSourceHandlerFixture : BaseHandlerFixture<FTPSourceHandler>
	{
		[Test(Description = "Тест попытки взаимодействия с ФТП при неправильном логине/пароле"), Ignore("Починить")]
		public void FtpInvalidLoginOrPassword()
		{
			source.FtpDir = "tml";
			source.FtpPassword = "123123123asd";
			source.FtpLogin = "test";
			source.PricePath = "ftp.ahold.comch.ru";
			source.Save();

			Process();

			CheckErrorMessage(priceItem, FtpSourceHandlerException.ErrorMessageInvalidLoginOrPassword);
		}

		[Test, Ignore("Починить")]
		public void FtpNetworkError()
		{
			source.PricePath = "ftp.ru1";
			source.Save();
			Process();
			CheckErrorMessage(priceItem, FtpSourceHandlerException.NetworkErrorMessage);
		}

		[Test, Ignore("Починить")]
		public void FtpChangePassiveModeTest()
		{
			source.PricePath = "217.173.73.200";
			source.PriceMask = "price.rar";
			source.ExtrMask = "price*.dbf";
			source.FtpLogin = "inforum";
			source.FtpPassword = "44Hr6FT3";
			source.FtpDir = "price";
			source.Save();

			Process();

			var sql = String.Format(@"select count(*) from `logs`.downlogs where PriceItemId = {0}", priceItem);
			var count = 0;
			With.Connection(connection => count = Convert.ToInt32(MySqlHelper.ExecuteScalar(connection, sql)));
			var files = Directory.GetFiles(Settings.Default.InboundPath);
			Assert.That(count, Is.EqualTo(1));
		}
	}
}