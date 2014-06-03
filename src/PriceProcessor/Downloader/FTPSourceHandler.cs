using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Net.Sockets;
using Common.Tools;
using Inforoom.PriceProcessor;
using log4net;
using LumiSoft.Net.FTP.Client;
using System.Threading;
using FileHelper = Common.Tools.FileHelper;

namespace Inforoom.Downloader.Ftp
{
	public class DownloadedFile
	{
		public DownloadedFile(string fileName, DateTime fileDate)
		{
			FileName = fileName;
			FileDate = fileDate;
		}

		public DownloadedFile(FileInfo fileName)
		{
			FileName = fileName.FullName;
			FileDate = fileName.LastWriteTime;
		}

		public string FileName { get; set; }
		public DateTime FileDate { get; set; }

		public override string ToString()
		{
			return String.Format("{0} {1}", FileName, FileDate);
		}
	}

	public class FailedFile
	{
		public string FileName { get; set; }
		public string ErrorMessage { get; set; }

		public FailedFile()
		{
		}

		public FailedFile(string fileName, Exception e)
		{
			FileName = fileName;
			ErrorMessage = e.ToString();
		}
	}

	public class FtpDownloader
	{
		public IList<FailedFile> FailedFiles = new List<FailedFile>();
		private static ILog _log = LogManager.GetLogger(typeof(FtpDownloader));

		/// <summary>
		/// Забирает файлы из фтп директории, сохраняет их локально и возвращает список локальных путей для этих файлов.
		/// Если при получении какого-то файла произошла ошибка, то пытается получить этот файл еще 2 раза, если не удалось,
		/// тогда имя этого файла добавляется в список FailedFiles.
		/// </summary>
		/// <param name="ftpHost">Имя хоста</param>
		/// <param name="ftpPort">Номер порта</param>
		/// <param name="ftpDirectory">Директория</param>
		/// <param name="username">Логин</param>
		/// <param name="password">Пароль</param>
		/// <param name="fileMask">Маска имени файла (на соответствие маске проверяется каждый файл)</param>
		/// <param name="lastDownloadTime">Время, когда была последняя загрузка</param>
		/// <param name="downloadDirectory">Директория, куда будут сохранены загруженные файлы</param>
		/// <param name="ftpPassiveMode">Пассивный режим для ftp-клиента</para>
		/// <returns>Список файлов, сохраненных локально</returns>
		public IList<DownloadedFile> GetFilesFromSource(string ftpHost, int ftpPort, string ftpDirectory, string username,
			string password, string fileMask, DateTime lastDownloadTime, string downloadDirectory, bool ftpPassiveMode = true)
		{
			var uri = new UriBuilder(ftpHost);
			ftpHost = uri.Host;
			if (!ftpDirectory.StartsWith(@"/", StringComparison.OrdinalIgnoreCase))
				ftpDirectory = @"/" + ftpDirectory;

			var receivedFiles = new List<DownloadedFile>();
			using (var ftpClient = new FTP_Client()) {
				_log.DebugFormat("Загрузка файлов с ftp://{0}@{1}:{2}/{3}/{4}", username, ftpHost, ftpPort, ftpDirectory, fileMask);
				var dataSetEntries = GetFtpFilesAndDirectories(ftpClient, ftpHost, ftpPort, username, password, ftpDirectory, ftpPassiveMode);
				foreach (DataRow entry in dataSetEntries.Tables["DirInfo"].Rows) {
					if (Convert.ToBoolean(entry["IsDirectory"]))
						continue;
					var fileInDirectory = entry["Name"].ToString();

					// Если файл не подходит по маске, берем следующий
					if (!PriceProcessor.FileHelper.CheckMask(fileInDirectory, fileMask))
						continue;

					var fileWriteTime = Convert.ToDateTime(entry["Date"]);
#if DEBUG
					lastDownloadTime = DateTime.Now.AddMonths(-1);
#endif

					if (((fileWriteTime.CompareTo(lastDownloadTime) > 0) &&
						(DateTime.Now.Subtract(fileWriteTime).TotalMinutes > Settings.Default.FileDownloadInterval)) ||
						((fileWriteTime.CompareTo(DateTime.Now) > 0) && (fileWriteTime.Subtract(lastDownloadTime).TotalMinutes > 0))) {
						var downloadedFile = Path.Combine(downloadDirectory, fileInDirectory);

						try {
#if !DEBUG
							ReceiveFile(ftpClient, fileInDirectory, downloadedFile);
#else
							// Для тестов
							if (ftpDirectory.StartsWith(@"/", StringComparison.OrdinalIgnoreCase))
								ftpDirectory = ftpDirectory.Substring(1);
							var path = Path.Combine(Settings.Default.FTPOptBoxPath, ftpDirectory);
							File.Copy(Path.Combine(path, fileInDirectory), downloadedFile, true);
#endif
							receivedFiles.Add(new DownloadedFile(downloadedFile, fileWriteTime));
							FileHelper.ClearReadOnly(downloadedFile);
						}
						catch (Exception e) {
							FailedFiles.Add(new FailedFile(fileInDirectory, e));
							_log.Debug("Ошибка при попытке загрузить файл с FTP поставщика", e);
						}
					}
					else {
						_log.DebugFormat("Файл {0} уже забран и дата файла еще не обновлена. Не забираем.", fileInDirectory);
					}
				}
#if !DEBUG
				ftpClient.Disconnect();
#endif
			}
			return receivedFiles;
		}

		private DataSet GetFtpFilesAndDirectories(FTP_Client ftpClient, string ftpHost, int ftpPort, string username, string password, string ftpDirectory, bool ftpPassiveMode = true)
		{
			DataSet dataSetEntries = null;
			ftpClient.PassiveMode = ftpPassiveMode;

#if !DEBUG
			ftpClient.Connect(ftpHost, ftpPort);
			ftpClient.Authenticate(username, password);
			ftpClient.SetCurrentDir(ftpDirectory);

			dataSetEntries = ftpClient.GetList();
#else
			dataSetEntries = ToDirInfo(Path.Combine(Settings.Default.FTPOptBoxPath, ftpDirectory), new DataSet());
#endif
			return dataSetEntries;
		}

		public static DataSet ToDirInfo(string dir, DataSet data)
		{
			var table = data.Tables["DirInfo"];
			if (!data.Tables.Contains("DirInfo")) {
				table = data.Tables.Add("DirInfo");
				table.Columns.Add("Name");
				table.Columns.Add("Date");
				table.Columns.Add("IsDirectory");
			}
			var files = Directory.GetFiles(dir, "*.*");
			foreach (var file in files) {
				var row = table.NewRow();
				row["Name"] = Path.GetFileName(file);
				row["Date"] = DateTime.Now.AddDays(-1);
				row["Size"] = new FileInfo(file).Length;
				row["IsDirectory"] = false;
				table.Rows.Add(row);
			}
			table.AcceptChanges();
			data.AcceptChanges();
			return data;
		}

		/// <summary>
		/// Пытается загрузить файл. После 3х неудачных попыток последнее исключение отдается наверх
		/// </summary>
		/// <param name="ftpClient">Объект FTP клиента</param>
		/// <param name="fileInDirectory">Имя файла в текущей FTP директории</param>
		/// <param name="downloadedFileName">Путь к файлу, куда он должен быть загружен</param>
		private void ReceiveFile(FTP_Client ftpClient, string fileInDirectory, string downloadedFileName)
		{
			var countAttempts = 3;

			for (var i = 0; i < countAttempts; i++) {
				try {
					if (File.Exists(downloadedFileName)) {
						_log.DebugFormat("Загрузка файла. Файл {0} уже существует. Удаляем", downloadedFileName);
						File.Delete(downloadedFileName);
					}
					using (var fileStream = new FileStream(downloadedFileName, FileMode.CreateNew)) {
						ftpClient.ReceiveFile(fileInDirectory, fileStream);
						return;
					}
				}
				catch (Exception) {
					if (i >= countAttempts)
						throw;
				}
			}
		}

		public DownloadedFile GetFileFromSource(PriceSource source, string downHandlerPath)
		{
			var ftpHost = source.PricePath;
			if (ftpHost.StartsWith(@"ftp://", StringComparison.OrdinalIgnoreCase))
				ftpHost = ftpHost.Substring(6);
			if (ftpHost.EndsWith(@"/"))
				ftpHost = ftpHost.Substring(0, ftpHost.Length - 1);

			var pricePath = source.FtpDir;
			if (!pricePath.StartsWith(@"/", StringComparison.OrdinalIgnoreCase))
				pricePath = @"/" + pricePath;

			var ftpFileName = String.Empty;
			var downFileName = String.Empty;
			var shortFileName = String.Empty;
			var priceDateTime = source.PriceDateTime;
			using (var ftpClient = new FTP_Client()) {
				ftpClient.PassiveMode = true;
				ftpClient.Connect(ftpHost, 21);
				ftpClient.Authenticate(source.FtpLogin, source.FtpPassword);
				ftpClient.SetCurrentDir(pricePath);

				var dsEntries = ftpClient.GetList();

				foreach (DataRow entry in dsEntries.Tables["DirInfo"].Rows) {
					if (Convert.ToBoolean(entry["IsDirectory"]))
						continue;

					shortFileName = entry["Name"].ToString();

					var priceMaskIsMatched = PriceProcessor.FileHelper.CheckMask(shortFileName, source.PriceMask);
					if (priceMaskIsMatched) {
						var fileLastWriteTime = Convert.ToDateTime(entry["Date"]);
#if DEBUG
						priceDateTime = fileLastWriteTime;
						ftpFileName = shortFileName;
#endif
						if (((fileLastWriteTime.CompareTo(priceDateTime) > 0)
							&& (DateTime.Now.Subtract(fileLastWriteTime).TotalMinutes > Settings.Default.FileDownloadInterval))
							|| ((fileLastWriteTime.CompareTo(DateTime.Now) > 0) && (fileLastWriteTime.Subtract(priceDateTime).TotalMinutes > 0))) {
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
			SourceType = "FTP";
		}

		protected override void GetFileFromSource(PriceSource source)
		{
			try {
				var downloader = new FtpDownloader();
				var file = downloader.GetFileFromSource(source, DownHandlerPath);
				if (file != null) {
					CurrFileName = file.FileName;
					CurrPriceDate = file.FileDate;
				}
			}
			catch (Exception e) {
				throw new FtpSourceHandlerException(e);
			}
		}

		public override DataRow[] GetLikeSources(PriceSource source)
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
		{
		}

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