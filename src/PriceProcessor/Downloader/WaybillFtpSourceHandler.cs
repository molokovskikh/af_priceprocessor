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
using Inforoom.Downloader;
using Inforoom.Downloader.Documents;
using Inforoom.PriceProcessor.Downloader.DocumentReaders;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.Downloader.Ftp;
using Inforoom.PriceProcessor.Waybills.Models;
using LumiSoft.Net.FTP.Client;
using NHibernate;

namespace Inforoom.PriceProcessor.Downloader
{
	public class WaybillFtpSourceHandler : AbstractHandler
	{
		private readonly InboundDocumentType[] _documentTypes;
		private List<object> _failedSources = new List<object>();
		private ISession session;

		public WaybillFtpSourceHandler()
		{
			_documentTypes = new InboundDocumentType[] { new WaybillType(), new RejectType() };
		}

		protected string GetSqlSources()
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
	st.LastDownload as LastDownloadTime,
	st.FtpActiveMode
FROM
	Documents.Waybill_Sources AS st
	JOIN Customers.Suppliers as s ON s.Id = st.FirmCode
	JOIN farm.regions as r ON r.RegionCode = s.HomeRegion
WHERE
	s.Disabled = 0
	AND st.SourceID = {0}
GROUP BY SupplierId
",
				Convert.ToUInt32(WaybillSourceType.FtpSupplier));
		}

		public override void ProcessData()
		{
			var ids = MySqlUtils.Fill(GetSqlSources()).AsEnumerable()
				.Select(r => Convert.ToUInt32(r["SupplierId"]))
				.ToList();

			foreach (var id in ids) {
				SessionHelper.StartSession(s => {
					try {
						session = s;
						ProcessSource(id);
					}
					finally {
						session = null;
					}
				});
			}
		}

		private void ProcessSource(uint id)
		{
			var source = session.Load<WaybillSource>(id);
			Cancellation.ThrowIfCancellationRequested();

			_logger.DebugFormat("Попытка забрать накладные с FTP поставщика. Код поставщика = {0}", source.Id);
			if (!source.IsReady) {
				_logger.DebugFormat("Пропускаю источник не истек период ожидания {0} дата последнего опроса {1}",
					source.DownloadInterval,
					source.LastDownload);
				return;
			}

			foreach (var documentType in _documentTypes) {
				ReceiveDocuments(documentType, source, source.Uri(documentType));
				// Удаление временных файлов
				Cleanup();
			}

			if (!_failedSources.Contains(source.Id)) {
				source.LastDownload = DateTime.Now;
			}
		}

		private void ReceiveDocuments(InboundDocumentType documentType, WaybillSource waybillSource, Uri uri)
		{
			_logger.InfoFormat("Попытка получения документов с FTP поставщика (код поставщика: {0}).\nТип документов: {1}.\nUrl: {2}",
				waybillSource.Id,
				documentType.DocType.GetDescription(),
				uri);

			var haveErrors = false;
			if (uri == null)
				return;

			try {
				using (var ftpClient = waybillSource.CreateFtpClient()) {
					ftpClient.Connect(uri.Host, uri.Port);
					ftpClient.Authenticate(waybillSource.UserName, waybillSource.Password);
					ftpClient.SetCurrentDir(uri.PathAndQuery);

					var files = ftpClient.GetList();
					foreach (var file in files.Tables["DirInfo"].AsEnumerable()) {
						if (Convert.ToBoolean(file["IsDirectory"]))
							continue;

						Cancellation.ThrowIfCancellationRequested();

						var source = file["Name"].ToString();
						var sourceDate = Convert.ToDateTime(file["Date"]);
						var sourceLength = Convert.ToInt64(file["Size"]);
						var dest = Path.Combine(DownHandlerPath, source);
						try {
							ftpClient.ReceiveFile(source, dest);
							var destLenth = new FileInfo(dest).Length;
							if (destLenth != sourceLength) {
								_logger.WarnFormat("Не совпадает размер загруженного файла {0} {1} размер на ftp {2} полученный рамер {3}",
									uri,
									source,
									sourceLength,
									destLenth);
								continue;
							}

							var downloadedFile = new DownloadedFile(dest, sourceDate);

							ProcessFile(documentType, waybillSource, downloadedFile);

							ftpClient.DeleteFile(source);
						}
						catch(Exception e) {
							haveErrors = true;
							_logger.Error(String.Format("Не удалось загрузить файл {0} с ftp {1}", source, uri), e);
						}
					}
				}

				if (!haveErrors && _failedSources.Contains(waybillSource.Id)) {
					waybillSource.LastError = DateTime.Now;
					_failedSources.Remove(waybillSource.Id);
					_logger.WarnFormat("После возникновения ошибок загрузка накладных прошла успешно. Код поставщика: {0}", waybillSource.Id);
				}
			}
			catch (Exception e) {
				var errorMessage = String.Format("Ошибка при попытке забрать документы с FTP поставщика (код поставщика: {0}).\nТип документов: {1}.\nUrl: {2}",
					waybillSource.Id,
					documentType.DocType.GetDescription(),
					uri);

				if (!_failedSources.Contains(waybillSource.Id)) {
					_failedSources.Add(waybillSource.Id);
					_logger.Warn(errorMessage, e);
				}
				else
					_logger.Debug(errorMessage, e);
			}
		}

		public void ProcessFile(InboundDocumentType documentType, WaybillSource waybillSource, DownloadedFile downloadedFile)
		{
			var logs = ProcessWaybill(documentType.DocType, waybillSource, downloadedFile);
			// Обработка накладной(или отказа), помещение ее в папку клиенту
			foreach (var log in logs)
				WaybillService.ParseWaybill(log);
		}

		private IEnumerable<DocumentReceiveLog> ProcessWaybill(DocType documentType, WaybillSource source, DownloadedFile downloadedFile)
		{
			var documentLogs = new List<DocumentReceiveLog>();
			var reader = new SupplierFtpReader();

			var addressIds = With.Connection(c => reader.ParseAddressIds(c, source.Id, downloadedFile.FileName, downloadedFile.FileName));

			foreach (var addressId in addressIds) {
				// Если накладная - это архив, разархивируем логируем каждый файл и копируем в папку клиенту
				var waybillFiles = new[] { downloadedFile.FileName };
				if (ArchiveHelper.IsArchive(downloadedFile.FileName)) {
					if (!ArchiveHelper.TestArchive(downloadedFile.FileName)) {
						_logger.DebugFormat("Некорректный архив {0}", downloadedFile.FileName);
						WaybillService.SaveWaybill(downloadedFile.FileName);
						continue;
					}
					// Разархивируем
					try {
						FileHelper.ExtractFromArhive(downloadedFile.FileName, downloadedFile.FileName + BaseSourceHandler.ExtrDirSuffix);
					}
					catch (ArchiveHelper.ArchiveException) {
						_logger.DebugFormat("Ошибка при извлечении файлов из архива {0}", downloadedFile.FileName);
						WaybillService.SaveWaybill(downloadedFile.FileName);
						continue;
					}
					if (ArchiveHelper.IsArchive(downloadedFile.FileName)) {
						// Получаем файлы, распакованные из архива
						waybillFiles = Directory.GetFiles(downloadedFile.FileName + BaseSourceHandler.ExtrDirSuffix + Path.DirectorySeparatorChar, "*.*",
							SearchOption.AllDirectories);
					}
				}

				foreach (var file in waybillFiles) {
					var isNew = IsNewWaybill(source, (uint)addressId, Path.GetFileName(file), new FileInfo(file).Length);
					if (!isNew) {
						_logger.DebugFormat("Файл {0} не является новой накладной, не обрабатываем его", file);
						continue;
					}
					var log = DocumentReceiveLog.LogNoCommit(source.Id,
						(uint)addressId,
						file,
						documentType,
						"Получен с клиентского FTP");
					_logger.InfoFormat("WaybillFtpSourceHandler: обработка файла {0}", file);
					documentLogs.Add(log);
				}
			}
			return documentLogs;
		}

		private bool IsNewWaybill(WaybillSource source,
			uint addressId,
			string filename,
			long filesize)
		{
			using (new SessionScope(FlushAction.Never)) {
				var docs = DocumentReceiveLog.Queryable
					.Where(d => d.Supplier.Id == source.Id
						&& d.FileName == filename
						&& d.DocumentSize == filesize
						&& d.Address.Id == addressId);

				return !docs.Any();
			}
		}
	}
}