using System;
using System.Collections.Generic;
using System.Text;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.Mime;
using Inforoom.Downloader.Properties;
using Inforoom.Formalizer;
using LumiSoft.Net.IMAP;


namespace Inforoom.Downloader
{
	public class WaybillSourceHandler : EMAILSourceHandler
	{

		public WaybillSourceHandler(string sourceType)
            : base(sourceType)
        { }

		protected override void IMAPAuth(IMAP_Client c)
		{
			c.Authenticate(Settings.Default.WaybillIMAPUser, Settings.Default.WaybillIMAPPass);
		}
	}
}
