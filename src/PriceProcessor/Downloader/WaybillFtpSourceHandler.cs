using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Common.MySql;
using Common.Tools;
using Inforoom.Common;
using Inforoom.Downloader;
using Inforoom.Downloader.DocumentReaders;
using Inforoom.Downloader.Documents;
using Inforoom.PriceProcessor.Downloader.DocumentReaders;
using Inforoom.PriceProcessor.Properties;
using Inforoom.PriceProcessor.Waybills;
using MySql.Data.MySqlClient;
using MySqlHelper = Common.MySql.MySqlHelper;
using Inforoom.Downloader.Ftp;

namespace Inforoom.PriceProcessor.Downloader
{
	public class WaybillSource
	{
		public uint SupplierId { get; set; }
		public string SupplierName { get; set; }
		public string RegionName { get; set; }
		public string WaybillUrl { get; set; }
		public string RejectUrl { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
		public uint DownloadInterval { get; set; }
		public DateTime LastDownloadTime { get; set; }

		public WaybillSource(DataRow row)
		{
			SupplierId = Convert.ToUInt32(row["SupplierId"]);
			SupplierName = row["SupplierName"].ToString();
			RegionName = row["RegionName"].ToString();
			WaybillUrl = row["WaybillUrl"].ToString();
			RejectUrl = row["RejectUrl"].ToString();
			UserName = row["UserName"].ToString();
			Password = row["Password"].ToString();
			DownloadInterval = Convert.IsDBNull(row["DownloadInterval"]) ? 0 : Convert.ToUInt32(row["DownloadInterval"]);
			LastDownloadTime = Convert.IsDBNull(row["LastDownloadTime"]) ? DateTime.MinValue : Convert.ToDateTime(row["LastDownloadTime"]);
		}
	}

	public class WaybillFtpSourceHandler : FTPSourceHandler
	{
		private readonly InboundDocumentType[] _documentTypes;

		public WaybillFtpSourceHandler()
		{
			_documentTypes = new InboundDocumentType[] { new WaybillType(), new RejectType() };
		}

		protected override string GetSQLSources()
		{
			return String.Format(@"
SELECT
	cd.FirmCode as SupplierId,
	cd.ShortName as SupplierName,
	r.Region as RegionName,
	st.WaybillUrl,
	st.RejectUrl,
	st.UserName,
	st.Password,
	st.DownloadInterval,
	max(dl.LogTime) as LastDownloadTime
FROM
	Documents.Waybill_Sources AS st
	INNER JOIN usersettings.ClientsData AS cd ON CD.FirmCode = st.FirmCode
	INNER JOIN farm.regions AS r ON r.RegionCode = cd.RegionCode
	LEFT JOIN logs.document_logs dl ON dl.FirmCode = st.FirmCode
WHERE
	cd.FirmStatus = 1
	AND st.SourceID = {0}
GROUP BY SupplierId
", Convert.ToUInt32(WaybillSourceType.FtpSupplier));
		}

		protected override void ProcessData()
		{
			FillSourcesTable();

			foreach (DataRow sourceRow in dtSources.Rows)
			{
				var waybillSource = new WaybillSource(sourceRow);
				if (!IsNeedToDownload(waybillSource))
					continue;

				foreach (var documentType in _documentTypes)
				{
					IList<DownloadedFile> downloadedWaybills = null;
					downloadedWaybills = ReceiveDocuments(documentType, waybillSource);
					foreach (var downloadedWaybill in downloadedWaybills)
					{
						CurrFileName = downloadedWaybill.FileName;

						// Обработка накладной(или отказа), помещение ее в папку клиенту
						var logs = ProcessWaybill(documentType.Type, waybillSource, downloadedWaybill);

						// Разбор накладных. Если скачанный файл является архивом, то к моменту разбора, во временной папке уже лежат разархивированные файлы
						foreach (var log in logs)
						{
							var fileName = downloadedWaybill.FileName;
							if (ArchiveHelper.IsArchive(downloadedWaybill.FileName))
								fileName = Directory.GetFiles(downloadedWaybill.FileName + ExtrDirSuffix, log.FileName, SearchOption.AllDirectories)[0];
							WaybillService.ParserDocument(log.Id, fileName);
						}

						// Удаление временных файлов
						Cleanup();
					}
				}
			}
		}

		private IList<DownloadedFile> ReceiveDocuments(InboundDocumentType documentsType, WaybillSource waybillSource)
		{
			var url = String.Empty;
			var host = String.Empty;
			var port = -1;
			var directory = String.Empty;
			IList<DownloadedFile> downloadedWaybills = new List<DownloadedFile>();

			var downloader = new FtpDownloader();
			try
			{
				if (documentsType.Type == DocType.Waybill)
					url = waybillSource.WaybillUrl;
				else if (documentsType.Type == DocType.Reject)
					url = waybillSource.RejectUrl;
				_log.DebugFormat("Попытка получения документов с FTP поставщика (код поставщика: {0}).\nТип документов: {1}.\nUrl: {2}",
					waybillSource.SupplierId, documentsType.Type.GetDescription(), url);
				if (String.IsNullOrEmpty(url))
					return downloadedWaybills;

				host = PathHelper.GetFtpHost(url);
				port = PathHelper.GetFtpPort(url);
				directory = PathHelper.GetFtpDirectory(url);

				downloadedWaybills = downloader.GetFilesFromSource(host, port, directory, waybillSource.UserName,
					waybillSource.Password, "*.*", waybillSource.LastDownloadTime, DownHandlerPath);

				if (FailedSources.Contains(waybillSource.SupplierId))
					FailedSources.Remove(waybillSource.SupplierId);
			}
			catch (Exception e)
			{
				var errorMessage = String.Format(@"Ошибка при попытке забрать документы с FTP поставщика
Код поставщика: {0}
Хост: {1}
Порт: {2}
Директория: {3}", waybillSource.SupplierId, host, port, directory);
				
				if (!FailedSources.Contains(waybillSource.SupplierId))
				{
					FailedSources.Add(waybillSource.SupplierId);
					_log.Error(errorMessage, e);
				}
				else
					_log.Debug(errorMessage, e);
			}

			if (FailedSources.Count > 0 || downloader.FailedFiles.Count > 0)
				SendDownloadingErrorMessages(downloader, waybillSource);

			return downloadedWaybills;
		}

		private bool IsNeedToDownload(WaybillSource source)
		{
			// downloadInterval - в секундах
			var downloadInterval = source.DownloadInterval;
			if (FailedSources.Contains(source.SupplierId))
			{
				FailedSources.Remove(source.SupplierId);
				downloadInterval = 0;
			}
			var seconds = DateTime.Now.Subtract(source.LastDownloadTime).TotalSeconds;
			if (seconds < downloadInterval)
				_log.Debug(String.Format("Для источника накладных еще не истек таймаут. Не забираем накладную (код поставщика: {0})", source.SupplierId));
			return (seconds >= downloadInterval);
		}

		private IList<DocumentLog> ProcessWaybill(DocType documentType, WaybillSource waybillSource, DownloadedFile waybill)
		{
			var documentLogs = new List<DocumentLog>();
			var reader = new GenezisPermReader();

			try
			{
				var addressIds = reader.GetClientCodes(_workConnection, waybillSource.SupplierId, waybill.FileName, waybill.FileName);

				foreach (var addressId in addressIds)
				{
					var addrId = (int?) addressId;
					var clientId = GetClientIdByAddress(ref addrId);
					if (clientId == null)
					{
						clientId = addrId;
						addrId = null;
					}

					// Если накладная - это архив, разархивируем логируем каждый файл и копируем в папку клиенту
					var waybillFiles = new string[0];
					if (ArchiveHelper.IsArchive(waybill.FileName))
					{
						if (!ArchiveHelper.TestArchive(waybill.FileName))
						{
							_log.DebugFormat("Некорректный архив {0}", waybill.FileName);
							WaybillService.SaveWaybill(waybill.FileName);
							continue;
						}
						// Разархивируем
						try
						{
							FileHelper.ExtractFromArhive(waybill.FileName, waybill.FileName + ExtrDirSuffix);
						}
						catch (ArchiveHelper.ArchiveException e)
						{
							_log.DebugFormat("Ошибка при извлечении файлов из архива {0}", waybill.FileName);
							WaybillService.SaveWaybill(waybill.FileName);
							continue;
						}
						if (ArchiveHelper.IsArchive(waybill.FileName))
						{
							// Получаем файлы, распакованные из архива
							waybillFiles = Directory.GetFiles(waybill.FileName + ExtrDirSuffix + Path.DirectorySeparatorChar, "*.*",
								SearchOption.AllDirectories);
						}
					}

					foreach (var file in waybillFiles)
					{
						if (!IsNewWaybill(Path.GetFileName(file), waybill.FileDate, waybillSource.SupplierId, clientId, addrId))
						{
							_log.DebugFormat("Файл {0} не является новой накладной, не обрабатываем его", file);
							continue;
						}
						var log = DocumentLog.Log(waybillSource.SupplierId, (uint?) clientId, (uint?) addrId,
						                          Path.GetFileName(file), documentType, null);
						if (!addrId.HasValue)
							addrId = clientId;
						CopyDocumentToClientDirectory(log, waybillSource.SupplierName, (uint)addrId, file, documentType);
						documentLogs.Add(log);
					}
				}
			}
			catch (Exception e)
			{
				_log.Error(String.Format("Ошибка при обработке документа, забранного с FTP поставщика"), e);
			}
			return documentLogs;
		}

		private bool IsNewWaybill(string waybillFileName, DateTime waybillDate, uint supplierId, int? clientId, int? addressId)
		{
			var count = 0;
			var filterByAddress = addressId.HasValue ? " and AddressId = ?AddressId" : String.Empty;

			var command = new MySqlCommand(String.Format(@"
SELECT count(*)
FROM logs.document_logs dl
WHERE dl.FirmCode = ?SupplierId and dl.FileName like ?FileName and dl.LogTime > ?WaybillDate and dl.ClientCode = ?ClientCode {0}
", filterByAddress), _workConnection);
			command.Parameters.AddWithValue("?FileName", waybillFileName);
			command.Parameters.AddWithValue("?WaybillDate", waybillDate);
			command.Parameters.AddWithValue("?ClientCode", clientId);
			command.Parameters.AddWithValue("?AddressId", addressId);
			command.Parameters.AddWithValue("?SupplierId", supplierId);
			count = Convert.ToInt32(command.ExecuteScalar());
			return count == 0;
		}

		private void SendDownloadingErrorMessages(FtpDownloader downloader, WaybillSource waybillSource)
		{
			try
			{
				var message = new StringBuilder();
				foreach (var failedFile in downloader.FailedFiles)
				{
					var failedEntry = String.Format("{0}_{1}", waybillSource.SupplierId, failedFile.FileName);
					if (FailedSources.Contains(failedEntry))
						continue;
					FailedSources.Add(failedEntry);
					message.AppendLine(String.Format("Файл '{0}'\n{1}", failedFile.FileName, failedFile.ErrorMessage));
				}

				if (!String.IsNullOrEmpty(message.ToString()))
					_log.ErrorFormat("При загрузке файлов с FTP поставщика возникли ошибки (код поставщика: {0})\n{1}",
					                 waybillSource.SupplierId, message);

				var restoredFilesMessage = new StringBuilder();
				foreach (var source in FailedSources)
				{
					var exists = downloader.FailedFiles.Where(file =>
						source.ToString().Contains(
							String.Format("{0}_{1}", waybillSource.SupplierId, file.FileName)
						)).Count() > 0;

					// Если файла нет в FailedFiles, но он еще есть в FailedSources, это значит что была ошибка, но теперь все хорошо
					// поэтому удаляем этот файл из FailedSources и пишем сообщение что он загружен
					if (!exists)
					{
						FailedSources.Remove(source);
						restoredFilesMessage.AppendLine(String.Format("Файл {0}", source.ToString().Split('_')[1]));
					}
				}
				
				if (!String.IsNullOrEmpty(restoredFilesMessage.ToString()))
					_log.InfoFormat(
						"После ошибок загрузки с FTP поставщика следующие документы были загружены (код поставщика: {0})\n{1}",
						waybillSource.SupplierId, restoredFilesMessage);
			}
			catch (Exception e)
			{
				_log.Error("При отправке сообщения об ошибках при загрузке накладных с FTP поставщика возникла ошибка", e);
			}
		}

		private void CopyDocumentToClientDirectory(DocumentLog documentLog, string supplierName, ulong addressId, string fileName, DocType documentType)
		{
			var clientDirectory = Common.FileHelper.NormalizeDir(Settings.Default.FTPOptBoxPath) +
				addressId.ToString().PadLeft(3, '0') + Path.DirectorySeparatorChar + documentType + "s";

			if (!Directory.Exists(clientDirectory))
				Directory.CreateDirectory(clientDirectory);

			var destinationFileName = documentLog.Id + "_" + supplierName + "(" + Path.GetFileNameWithoutExtension(fileName) + ")" + Path.GetExtension(fileName);
			destinationFileName = Path.Combine(clientDirectory, destinationFileName);

			File.Copy(fileName, destinationFileName, true);
		}
	}
}
