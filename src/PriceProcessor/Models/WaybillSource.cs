using System;
using Castle.ActiveRecord;
using Inforoom.Downloader.Documents;
using LumiSoft.Net.FTP.Client;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("Waybill_Sources", Schema = "Documents", DynamicUpdate = true)]
	public class WaybillSource
	{
		[PrimaryKey("FirmCode")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual DateTime? LastDownload { get; set; }

		[Property]
		public virtual DateTime? LastError { get; set; }

		[Property]
		public virtual int? DownloadInterval { get; set; }

		[Property]
		public virtual bool FtpActiveMode { get; set; }

		[Property]
		public virtual string UserName { get; set; }

		[Property]
		public virtual string Password { get; set; }

		[Property]
		public virtual string WaybillUrl { get; set; }

		[Property]
		public virtual string RejectUrl { get; set; }

		public virtual bool IsReady
		{
			get
			{
				// downloadInterval - в секундах
				var downloadInterval = DownloadInterval.GetValueOrDefault();
				if (LastDownload == null)
					return true;
				if (LastError > LastDownload)
					return true;
				var seconds = DateTime.Now.Subtract(LastDownload.Value).TotalSeconds;
				return seconds >= downloadInterval;
			}
		}

		public Uri Uri(InboundDocumentType documentType)
		{
			string data = null;
			if (documentType is WaybillType) {
				data = WaybillUrl;
			}
			if (documentType is RejectType) {
				data = RejectUrl;
			}
			if (String.IsNullOrEmpty(data))
				return null;

			var uri = new Uri(WaybillUrl, UriKind.RelativeOrAbsolute);
			if (!uri.IsAbsoluteUri)
				uri = new UriBuilder("ftp" + System.Uri.SchemeDelimiter + WaybillUrl).Uri;
			return uri;
		}

		public FTP_Client CreateFtpClient()
		{
			var client = new FTP_Client();
			client.PassiveMode = !FtpActiveMode;
			return client;
		}
	}
}