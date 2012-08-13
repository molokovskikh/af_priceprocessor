using System;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Models
{
	[TestFixture]
	public class DocumentReceiveLogFixture
	{
		[Test]
		public void Check_client_region()
		{
			var supplier = new Supplier {
				RegionMask = 2
			};
			var address = new Address {
				Client = new Client {
					MaskRegion = 1
				}
			};
			var log = new DocumentReceiveLog(supplier, address);
			Assert.Catch<EMailSourceHandlerException>(log.Check);
		}
	}
}