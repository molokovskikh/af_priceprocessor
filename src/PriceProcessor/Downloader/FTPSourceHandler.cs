 using System;
using System.IO;
using System.Data;
using LumiSoft.Net.FTP.Client;
using Inforoom.PriceProcessor.Properties;
using Inforoom.Common;

namespace Inforoom.Downloader
{
	public class DownloadedFile
	{
		public DownloadedFile(string fileName, DateTime fileDate)
		{
			FileName = fileName;
			FileDate = fileDate;
		}

		public string FileName { get; set; }
		public DateTime FileDate { get; set; }

		public override string ToString()
		{
			return String.Format("{0} {1}", FileName, FileDate);
		}
	}

	public class FtpDownloader
	{
		public DownloadedFile GetFileFromSource(PriceSource source, string downHandlerPath)
		{
			var ftpHost = source.PricePath;
			if (ftpHost.StartsWith(@"ftp://", StringComparison.OrdinalIgnoreCase))
				ftpHost = ftpHost.Substring(6);
			if (ftpHost.EndsWith(@"/"))
				ftpHost = ftpHost.Substring(0, ftpHost.Length-1);

			var pricePath = source.FtpDir;
			if (!pricePath.StartsWith(@"/", StringComparison.OrdinalIgnoreCase))
				pricePath = @"/" + pricePath;

			var ftpFileName = String.Empty;
			var downFileName = String.Empty;
			var shortFileName = String.Empty;
			var priceDateTime = source.PriceDateTime;
			using (var ftpClient = new FTP_Client())
			{
				ftpClient.PassiveMode = true;
				ftpClient.Connect(ftpHost, 21);
				ftpClient.Authenticate(source.FtpLogin, source.FtpPassword);
				ftpClient.SetCurrentDir(pricePath);

				var dsEntries = ftpClient.GetList();

				foreach (DataRow drEnt in dsEntries.Tables["DirInfo"].Rows)
				{
					if (Convert.ToBoolean(drEnt["IsDirectory"]))
						continue;

					shortFileName = drEnt["Name"].ToString();
					if ((WildcardsHelper.IsWildcards(source.PriceMask) && WildcardsHelper.Matched(source.PriceMask, shortFileName)) ||
						(String.Compare(shortFileName, source.PriceMask, true) == 0))
					{
						var fileLastWriteTime = Convert.ToDateTime(drEnt["Date"]);
						if (((fileLastWriteTime.CompareTo(priceDateTime) > 0)
								&& (DateTime.Now.Subtract(fileLastWriteTime).TotalMinutes > Settings.Default.FileDownloadInterval))
								|| ((fileLastWriteTime.CompareTo(DateTime.Now) > 0) && (fileLastWriteTime.Subtract(priceDateTime).TotalMinutes > 0)))
						{
							priceDateTime = fileLastWriteTime;
							ftpFileName = shortFileName;
						}
					}
				}

				if (String.IsNullOrEmpty(ftpFileName))
					return null;
				downFileName = Path.Combine(downHandlerPath, ftpFileName);
				using (var file = new FileStream(downFileName, FileMode.Create))
					ftpClient.ReceiveFile(ftpFileName, file);
			}

			return new DownloadedFile(downFileName, priceDateTime);
		}

		public DataRow[] GetLikeSources(DataTable sources, PriceSource source)
		{
			return sources.Select(String.Format("({0} = '{1}') and ({2} = '{3}') and (ISNULL({4}, '') = '{5}') and (ISNULL({6}, '') = '{7}') and (ISNULL({8}, '') = '{9}')",
				SourcesTableColumns.colPricePath, source.PricePath,
				SourcesTableColumns.colPriceMask, source.PriceMask,
				SourcesTableColumns.colFTPDir, source.FtpDir,
				SourcesTableColumns.colFTPLogin, source.FtpLogin,
				SourcesTableColumns.colFTPPassword, source.FtpPassword));
		}
	}

	public class FTPSourceHandler : PathSourceHandler
	{
		public FTPSourceHandler()
		{
			sourceType = "FTP";
		}

		protected override void GetFileFromSource(PriceSource source)
		{
			var downloader = new FtpDownloader();
			var file = downloader.GetFileFromSource(source, DownHandlerPath);
			if (file != null)
			{
				CurrFileName = file.FileName;
				CurrPriceDate = file.FileDate;
			}
		}

		protected override DataRow[] GetLikeSources(PriceSource source)
		{
			var downloader = new FtpDownloader();
			return downloader.GetLikeSources(dtSources, source);
		}
	}
}
