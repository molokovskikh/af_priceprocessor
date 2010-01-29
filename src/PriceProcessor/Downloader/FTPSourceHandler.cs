 using System;
using System.IO;
using System.Data;
using System.Net.Sockets;
using Inforoom.PriceProcessor;
using LumiSoft.Net.FTP;
using LumiSoft.Net.FTP.Client;
using Inforoom.PriceProcessor.Properties;
using Inforoom.Common;
using LumiSoft.Net;
using System.Threading;

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
	{	/*	
		private const int FtpPort = 21;

		private FTP_ListItem[] GetList(FTP_Client ftpClient, string ftpHost, string ftpDir, PriceSource priceSource)
		{
			ftpClient.Connect(ftpHost, FtpPort);
			ftpClient.Authenticate(priceSource.FtpLogin, priceSource.FtpPassword);
			ftpClient.SetCurrentDir(ftpDir);
			FTP_ListItem[] items = null;
			try
			{
				items = ftpClient.GetList();
			}
			catch (IOException)
			{
				items = ftpClient.GetList();
			}
			return items;
		}

		private FTP_ListItem[] FtpClientGetList(FTP_Client ftpClient, PriceSource priceSource, string ftpHost, string ftpDir)
		{
			FTP_ListItem[] entries = null;
			try
			{
				ftpClient.TransferMode = priceSource.FtpPassiveMode ? FTP_TransferMode.Passive : FTP_TransferMode.Active;
				entries = GetList(ftpClient, ftpHost, ftpDir, priceSource);
			}
			catch (FTP_ClientException e)
			{
				if ((e.StatusCode == (int)FTP_StatusCode.ServiceNotAvaliable) || (e.StatusCode == (int)FTP_StatusCode.ConnectionTimeout))
				{
					ftpClient.Disconnect();
					ftpClient.TransferMode = (!priceSource.FtpPassiveMode) ? FTP_TransferMode.Passive : FTP_TransferMode.Active;
					entries = GetList(ftpClient, ftpHost, ftpDir, priceSource);
					var warningMessageBody = String.Format(
@"При попытке подключиться к {0} с FTPPassiveMode = {1} возникла ошибка.
Значение FTPPassiveMode было изменено на {2}. Подключение прошло успешно.
Возможно, неверное значение поля FTPPassiveMode в таблице farm.Sources.(PriceItemId = {3}, FirmCode = {4})
Ошибка:
{5}", ftpHost, priceSource.FtpPassiveMode, !priceSource.FtpPassiveMode, priceSource.PriceItemId, priceSource.FirmCode, e);
					Mailer.SendFromServiceToService("Предупреждение в PriceProcessor", warningMessageBody);
				}
				else
					throw;
			}
			return entries;
		}
		/**/
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
				//var dsEntries = FtpClientGetList(ftpClient, source, ftpHost, pricePath);
				ftpClient.PassiveMode = true;
				ftpClient.Connect(ftpHost, 21);
				ftpClient.Authenticate(source.FtpLogin, source.FtpPassword);
				ftpClient.SetCurrentDir(pricePath);

				var dsEntries = ftpClient.GetList();

				foreach (DataRow entry in dsEntries.Tables["DirInfo"].Rows)
				{
					if (Convert.ToBoolean(entry["IsDirectory"]))
						continue;

					//shortFileName = entry.Name;
					shortFileName = entry["Name"].ToString();
					if ((WildcardsHelper.IsWildcards(source.PriceMask) && WildcardsHelper.Matched(source.PriceMask, shortFileName)) ||
						(String.Compare(shortFileName, source.PriceMask, true) == 0))
					{
						//var fileLastWriteTime = entry.Modified;
						var fileLastWriteTime = Convert.ToDateTime(entry["Date"]);
#if DEBUG
						priceDateTime = fileLastWriteTime;
						ftpFileName = shortFileName;
#endif
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
					//ftpClient.GetFile(ftpFileName, file);
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
			try
			{
				var downloader = new FtpDownloader();
				var file = downloader.GetFileFromSource(source, DownHandlerPath);
				if (file != null)
				{
					CurrFileName = file.FileName;
					CurrPriceDate = file.FileDate;
				}
			}
			catch (Exception e)
			{
				throw new FtpSourceHandlerException(e);
			}
		}

		protected override DataRow[] GetLikeSources(PriceSource source)
		{
			var downloader = new FtpDownloader();
			return downloader.GetLikeSources(dtSources, source);
		}
	}

	public class FtpSourceHandlerException : PathSourceHandlerException
	{
		public static string ErrorMessageInvalidLoginOrPassword = "Неправильный логин/пароль.";
		public static string ErrorMessageServiceNotAvaliable = "Сервис недоступен.";

		public FtpSourceHandlerException()
		{ }

		public FtpSourceHandlerException(Exception innerException)
			: base(null, innerException)
		{
			ErrorMessage = GetShortErrorMessage(innerException);
		}

		protected override string GetShortErrorMessage(Exception e)
		{
			var message = String.Empty;
			//var ftpClientException = e as FTP_ClientException;
			var socketException = e as SocketException;
			var threadAbortException = e as ThreadAbortException;
			/*
			if (ftpClientException != null)
			{
				switch (ftpClientException.StatusCode)
				{
					case (int)FTP_StatusCode.UserNotLoggedIn:
						{
							message += ErrorMessageInvalidLoginOrPassword;
							break;
						}
					case (int)FTP_StatusCode.ServiceNotAvaliable:
						{
							message += ErrorMessageServiceNotAvaliable;
							break;
						}
					default:				
						{
							message += NetworkErrorMessage;
							break;
						}
				}
			}
			else*/
			if (socketException != null)
				return NetworkErrorMessage;
			if (threadAbortException != null)
				return ThreadAbortErrorMessage;
			return message;
		}
	}
}
