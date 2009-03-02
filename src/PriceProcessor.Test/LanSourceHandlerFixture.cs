using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Common.Tools;
using Inforoom.Downloader;
using Inforoom.PriceProcessor;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class LanSourceHandlerFixture
	{
		[SetUp]
		public void Setup()
		{
			InitDirs("FTPOptBox", "Inbound0");
		}

		private static void InitDirs(params string[] dirs)
		{
			dirs.Each(dir => {
			          	if (Directory.Exists(dir))
							Directory.Delete(dir, true);
			          	Directory.CreateDirectory(dir);
			          });
		}

		[Test]
		public void After_price_download_last_download_date_should_be_updated()
		{
//            var begin = DateTime.Now;
//            var handler = new LANSourceHandler();
//            handler.StartWork();
//            File.Copy(@"Data\552.dbf", @"FTPOptBox\552.dbf");
//            Thread.Sleep(30000);
//            handler.StopWork();
//            Assert.That(File.Exists(@"Inbound0\552.dbf"), Is.True);
//            Assert.That(MySqlHelper.ExecuteScalar(Literals.ConnectionString(), @"
//select LastDownload
//from usersettings.priceitems 
//where id = 552"), Is.GreaterThan(begin));
		}

		[Test]
		public void Do_not_down_load_price_if_file_was_not_updated()
		{

		}
	}
}
