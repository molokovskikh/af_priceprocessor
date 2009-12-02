using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LumiSoft.Net.IMAP;
using NUnit.Framework;
using Inforoom.Downloader;
using Inforoom.PriceProcessor.Properties;
using System.Threading;
using System.IO;
using LumiSoft.Net.IMAP.Client;
using Inforoom.Downloader.Documents;
using MySql.Data.MySqlClient;


namespace PriceProcessor.Test
{
	[TestFixture]
	public class WaybillLANSourceHandlerFixture
	{
		[Test]
		public void TestWaybillLANSourceHandler()
		{
			var handler = new WaybillLANSourceHandler();
			handler.StartWork();
			Thread.Sleep(500000000);
			handler.StopWork();
		}
	}
}
