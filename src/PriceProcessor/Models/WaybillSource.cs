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
			if (documentType is WaybillType) {
				if (String.IsNullOrEmpty(WaybillUrl))
					return null;
				return new Uri(WaybillUrl);
			}
			if (documentType is RejectType) {
				if (String.IsNullOrEmpty(RejectUrl))
					return null;
				return new Uri(RejectUrl);
			}
			return null;
		}

		public FTP_Client CreateFtpClient()
		{
			var client = new FTP_Client();
			client.PassiveMode = !FtpActiveMode;
			return client;
		}
	}
}