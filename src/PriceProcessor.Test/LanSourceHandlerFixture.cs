using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class LanSourceHandlerFixture
	{
		[SetUp]
		public void Setup()
		{
			TestHelper.InitDirs("FTPOptBox", "Inbound0");
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
