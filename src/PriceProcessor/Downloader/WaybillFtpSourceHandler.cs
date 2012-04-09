using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.MySql;
using Common.Tools;
using Inforoom.Common;
using Inforoom.Downloader.Documents;
using Inforoom.PriceProcessor.Downloader.DocumentReaders;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.Downloader.Ftp;
using Inforoom.PriceProcessor.Waybills.Models;

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
	s.Id as SupplierId,
	s.Name as SupplierName,
	r.Region as RegionName,
	st.WaybillUrl,
	st.RejectUrl,
	st.UserName,
	st.Password,
	st.DownloadInterval,
	max(dl.LogTime) as LastDownloadTime
FROM
	Documents.Waybill_Sources AS st
	JOIN Customers.Suppliers as s ON s.Id = st.FirmCode
	JOIN farm.regions as r ON r.RegionCode = s.HomeRegion
	LEFT JOIN logs.document_logs dl ON dl.FirmCode = st.FirmCode
WHERE
	s.Disabled = 0
	AND st.SourceID = {0}
GROUP BY SupplierId
", Convert.ToUInt32(WaybillSourceType.FtpSupplier));
		}

		public override void ProcessData()
		{
			FillSourcesTable();
			foreach (DataRow sourceRow in dtSources.Rows)
			{
				var waybillSource = new WaybillSource(sourceRow);
				_log.InfoFormat("Попытка забрать накладные с FTP поставщика. Код поставщика = {0}", waybillSource.SupplierId);
				if (!IsNeedToDownload(waybillSource))
					continue;

				foreach (var documentType in _documentTypes)
				{
					var downloadedWaybills = ReceiveDocuments(documentType, waybillSource);
					foreach (var downloadedWaybill in downloadedWaybills)
					{
						CurrFileName = downloadedWaybill.FileName;

						// Обработка накладной(или отказа), помещение ее в папку клиенту
						var logs = ProcessWaybill(documentType.DocType, waybillSource, downloadedWaybill);

						foreach (var log in logs)
							WaybillService.ParserDocument(log);

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
				if (documentsType.DocType == DocType.Waybill)
					url = waybillSource.WaybillUrl;
				else if (documentsType.DocType == DocType.Reject)
					url = waybillSource.RejectUrl;
				_log.InfoFormat("Попытка получения документов с FTP поставщика (код поставщика: {0}).\nТип документов: {1}.\nUrl: {2}",
					waybillSource.SupplierId, documentsType.DocType.GetDescription(), url);
				if (String.IsNullOrEmpty(url))
					return downloadedWaybills;

				host = PathHelper.GetFtpHost(url);
				port = PathHelper.GetFtpPort(url);
				directory = PathHelper.GetFtpDirectory(url);

				downloadedWaybills = downloader.GetFilesFromSource(host, port, directory, waybillSource.UserName,
					waybillSource.Password, "*.*", waybillSource.LastDownloadTime, DownHandlerPath);

				if (FailedSources.Contains(waybillSource.SupplierId))
				{
					FailedSources.Remove(waybillSource.SupplierId);
					_log.ErrorFormat("После возникновения ошибок загрузка накладных прошла успешно. Код поставщика: {0}", waybillSource.SupplierId);
				}
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

			if (downloader.FailedFiles.Count > 0)
				SendDownloadingErrorMessages(downloader, waybillSource);

			return downloadedWaybills;
		}

		public bool IsNeedToDownload(WaybillSource source)
		{
			// downloadInterval - в секундах
			var downloadInterval = source.DownloadInterval;
			if (FailedSources.Contains(source.SupplierId))
				downloadInterval = 0;
			var seconds = DateTime.Now.Subtract(source.LastDownloadTime).TotalSeconds;
			if (seconds < downloadInterval)
				_log.Debug(String.Format("Для источника накладных еще не истек таймаут. Не забираем накладную (код поставщика: {0})", source.SupplierId));
			return (seconds >= downloadInterval);
		}

		private IEnumerable<DocumentReceiveLog> ProcessWaybill(DocType documentType, WaybillSource waybillSource, DownloadedFile waybill)
		{
			var documentLogs = new List<DocumentReceiveLog>();
			var reader = new SupplierFtpReader();

			try
			{
				var addressIds = With.Connection(c => reader.GetClientCodes(c, waybillSource.SupplierId, waybill.FileName, waybill.FileName));

				foreach (var addressId in addressIds)
				{
					var addrId = (uint?) addressId;
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
						catch (ArchiveHelper.ArchiveException)
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
						var log = DocumentReceiveLog.Log(waybillSource.SupplierId,
							clientId,
							addrId,
							file,
							documentType,
							"Получен с клиентского FTP");
						_logger.InfoFormat("WaybillFtpSourceHandler: обработка файла {0}", file);
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

		private bool IsNewWaybill(string waybillFileName, DateTime waybillDate, uint supplierId, uint? clientId, uint? addressId)
		{
			using (new SessionScope(FlushAction.Never))
			{
				var docs = DocumentReceiveLog.Queryable
					.Where(d => d.Supplier.Id == supplierId
						&& d.FileName == waybillFileName
						&& d.LogTime > waybillDate
						&& d.ClientCode == clientId);
				if (addressId.HasValue)
				{
					var id = addressId.Value;
					docs = docs.Where(d => d.Address.Id == id);
				}

				var count = docs.Count();
				return count == 0;
			}
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
					_log.ErrorFormat(
						"После ошибок загрузки с FTP поставщика следующие документы были загружены (код поставщика: {0})\n{1}",
						waybillSource.SupplierId, restoredFilesMessage);
			}
			catch (Exception e)
			{
				_log.Error("При отправке сообщения об ошибках при загрузке накладных с FTP поставщика возникла ошибка", e);
			}
		}
	}
}
