using Inforoom.Downloader.Documents;
using Inforoom.PriceProcessor.Models;
using NUnit.Framework;

namespace PriceProcessor.Test.Models
{
	[TestFixture]
	public class WaybillSourceFixture
	{
		[Test]
		public void Set_active_mode()
		{
			var source = new WaybillSource();
			var client = source.CreateFtpClient();
			Assert.That(client.PassiveMode, Is.True);

			source.FtpActiveMode = true;
			client = source.CreateFtpClient();
			Assert.That(client.PassiveMode, Is.False);
		}

		[Test]
		public void Parse_uri()
		{
			var source = new WaybillSource();
			source.WaybillUrl = "ftp.oriola-russia.ru/Nakl/";
			var uri = source.Uri(new WaybillType());
			Assert.That(uri.ToString(), Is.EqualTo("ftp://ftp.oriola-russia.ru/Nakl/"));
			Assert.That(uri.Host, Is.EqualTo("ftp.oriola-russia.ru"));
		}

		[Test]
		public void Get_source()
		{
			var source = new WaybillSource {
				WaybillUrl = "ftp2.yarfarma.ru/invoice/",
				RejectUrl = "ftp2.yarfarma.ru/deny/"
			};
			Assert.AreEqual("ftp://ftp2.yarfarma.ru/deny/", source.Uri(new RejectType()).ToString());
		}
	}
}