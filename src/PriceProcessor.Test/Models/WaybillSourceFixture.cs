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
	}
}