using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using Castle.ActiveRecord;
using Common.MySql;
using Common.Tools;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor;
using Inforoom.Downloader.DocumentReaders;
using Inforoom.Downloader.Documents;
using System.Net.Mail;
using System.Reflection;
using Inforoom.Common;
using Inforoom.PriceProcessor.Waybills.Models;
using Attachment = System.Net.Mail.Attachment;
using FileHelper = Common.Tools.FileHelper;

namespace Inforoom.Downloader
{
	public class WaybillLanSourceHandler : BaseSourceHandler
	{
		private readonly InboundDocumentType[] _documentTypes;
		protected InboundDocumentType _currentDocumentType;

		public WaybillLanSourceHandler()
		{
			SourceType = "WAYBILLLAN";
			_documentTypes = new InboundDocumentType[] { new WaybillType(), new RejectType() };
		}

		// Выбирает данные о включенных поставщиках, накладные от которых обрабатываются особым образом
		protected override string GetSQLSources()
		{
			return @"
SELECT
  s.Id as FirmCode,
  s.Name as ShortName,
  st.EMailFrom,
  st.ReaderClassName
FROM
  Customers.Suppliers as s
  INNER JOIN Documents.Waybill_Sources AS st ON s.Id = st.FirmCode
WHERE
s.Disabled = 0
and st.SourceID = 4";
		}

		public override void ProcessData()
		{
			//набор строк похожих источников
			DataRow drLanSource;
			// Заполняем таблицу с данными о поставщиках.
			FillSourcesTable();

			while (dtSources.Rows.Count > 0) {
				try {
					_currentDocumentType = null;
					// Берем нулевую строку (с данными о поставщике)
					drLanSource = dtSources.Rows[0];

					var clazz = drLanSource[WaybillSourcesTable.colReaderClassName].ToString();
					if (String.IsNullOrEmpty(clazz))
						continue;
					var documentReader = ReflectionHelper.GetDocumentReader<BaseDocumentReader>(clazz);

					foreach (var documentType in _documentTypes)
						try {
							_currentDocumentType = documentType;

							// Получаем список файлов из папки
							var files = GetFileFromSource(documentReader);

							foreach (var sourceFileName in files) {
								GetCurrentFile(sourceFileName);

								if (!String.IsNullOrEmpty(CurrFileName)) {
									var CorrectArchive = true;
									//Является ли скачанный файл корректным, если нет, то обрабатывать не будем
									if (ArchiveHelper.IsArchive(CurrFileName)) {
										if (ArchiveHelper.TestArchive(CurrFileName)) {
											try {
												PriceProcessor.FileHelper.ExtractFromArhive(CurrFileName, CurrFileName + ExtrDirSuffix);
											}
											catch (ArchiveHelper.ArchiveException) {
												CorrectArchive = false;
											}
										}
										else
											CorrectArchive = false;
									}

									if (CorrectArchive) {
										if (!ProcessWaybillFile(CurrFileName, drLanSource, documentReader)) {
											using (var mm = new MailMessage(
												Settings.Default.FarmSystemEmail,
												Settings.Default.DocumentFailMail,
												String.Format("{0} ({1})", drLanSource[WaybillSourcesTable.colShortName], SourceType),
												String.Format("Код поставщика : {0}\nФирма: {1}\nТип: {2}\nДата: {3}\nПричина: {4}",
													drLanSource[WaybillSourcesTable.colFirmCode],
													drLanSource[SourcesTableColumns.colShortName],
													_currentDocumentType.GetType().Name,
													DateTime.Now,
													"Не удалось сопоставить документ клиентам. Подробнее смотрите в таблице logs.document_logs."))) {
												if (!String.IsNullOrEmpty(CurrFileName))
													mm.Attachments.Add(new Attachment(CurrFileName));
												var sc = new SmtpClient(Settings.Default.SMTPHost);
												sc.Send(mm);
											}
										}
										//После обработки файла удаляем его из папки
										if (!String.IsNullOrEmpty(sourceFileName) && File.Exists(sourceFileName))
											File.Delete(sourceFileName);
									}
									else {
										var supplierId = Convert.ToUInt32(drLanSource[WaybillSourcesTable.colFirmCode]);
										DocumentReceiveLog.Log(supplierId, null, Path.GetFileName(CurrFileName), documentType.DocType, String.Format("Не удалось распаковать файл '{0}'", Path.GetFileName(CurrFileName)));
										//Распаковать файл не удалось, поэтому удаляем его из папки
										if (!String.IsNullOrEmpty(sourceFileName) && File.Exists(sourceFileName))
											File.Delete(sourceFileName);
									}
									Cleanup();
								}
							}
						}
						catch (Exception e) {
							//Обрабатываем ошибку в случае обработки одного из типов документов
							var message = String.Format("Источник : {0}\nТип : {1}", dtSources.Rows[0][WaybillSourcesTable.colFirmCode], documentType.GetType().Name);
							Log(e, message);
						}


					drLanSource.Delete();
					dtSources.AcceptChanges();
				}
				catch (Exception ex) {
					var error = String.Format("Источник : {0}", dtSources.Rows[0][WaybillSourcesTable.colFirmCode]);
					Log(ex, error);
					try {
						dtSources.Rows[0].Delete();
					}
					catch {
					}
					try {
						dtSources.AcceptChanges();
					}
					catch {
					}
				}
			}
		}

		protected string[] GetFileFromSource(BaseDocumentReader documentReader)
		{
			var pricePath = String.Empty;
			try {
				// Путь к папке, из которой нужно забирать накладную
				// \FTPOptBox\<Код постащика>\Waybills\ (или \Rejects\)
				pricePath = Path.Combine(Settings.Default.FTPOptBoxPath,
					dtSources.Rows[0]["FirmCode"].ToString().PadLeft(3, '0'),
					_currentDocumentType.FolderName);
				// Получаем все файлы из этой папки
				var ff = Directory.GetFiles(pricePath);
				// Отсекаем файлы с некорректным расширением
				var newFiles = new List<string>();

				//задержка что бы избежать канликтов в dfs
				Thread.Sleep(500);

				foreach (var newFileName in ff) {
					if (Array.Exists(documentReader.ExcludeExtentions,
						s => s.Equals(Path.GetExtension(newFileName), StringComparison.OrdinalIgnoreCase))) {
						// Если есть файл с некорректным разрешением, удаляем его
						if (File.Exists(newFileName))
							File.Delete(newFileName);
					}
					else if (DateTime.Now.Subtract(File.GetLastWriteTime(newFileName)).TotalMinutes > Settings.Default.FileDownloadInterval)
						newFiles.Add(newFileName);
				}
				return documentReader.UnionFiles(newFiles.ToArray());
			}
			catch (Exception e) {
				Log(e,
					String.Format("Не удалось получить список файлов для папки {0}", pricePath));
				return new string[] { };
			}
		}

		private void GetCurrentFile(string sourceFile)
		{
			CurrFileName = String.Empty;
			var destination = DownHandlerPath + Path.GetFileName(sourceFile);
			try {
				if (File.Exists(destination))
					File.Delete(destination);
				FileHelper.ClearReadOnly(sourceFile);
				File.Copy(sourceFile, destination);
				CurrFileName = destination;
			}
			catch (Exception ex) {
				Log(ex, String.Format("Не удалось скопировать файл {0} в {1}", sourceFile, destination));
			}
		}

		protected bool ProcessWaybillFile(string inFile, DataRow drCurrent, BaseDocumentReader documentReader)
		{
			//Массив файлов
			var files = new[] { inFile };
			var dir = inFile + ExtrDirSuffix;
			if (ArchiveHelper.IsArchive(inFile)) {
				// Получаем файлы, распакованные из архива
				files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
			}

			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			try {
				files = documentReader.DivideFiles(dir, files);
			}
			catch (Exception exDivide) {
				var supplierId = Convert.ToUInt32(drCurrent[WaybillSourcesTable.colFirmCode]);
				DocumentReceiveLog.Log(supplierId, null, Path.GetFileName(CurrFileName), _currentDocumentType.DocType, String.Format("Не удалось разделить файлы: {0}", exDivide));
				return false;
			}

			var processed = false;

			foreach (var file in files) {
				if (MoveWaybill(inFile, file, drCurrent, documentReader))
					processed = true;
			}
			return processed;
		}

		protected bool MoveWaybill(string archFileName, string fileName, DataRow drCurrent, BaseDocumentReader documentReader)
		{
			using (var cleaner = new FileCleaner()) {
				var supplierId = Convert.ToUInt32(drCurrent[WaybillSourcesTable.colFirmCode]);
				try {
					var addresses = With.Connection(c => documentReader.ParseAddressIds(c, supplierId, archFileName, fileName));
					var formatFile = documentReader.FormatOutputFile(fileName, drCurrent);

					cleaner.Watch(fileName);
					cleaner.Watch(formatFile);

					foreach (var addressId in addresses) {
						var log = DocumentReceiveLog.LogNoCommit(supplierId,
							(uint)addressId,
							formatFile,
							_currentDocumentType.DocType,
							"Получен с нашего FTP");

						_logger.InfoFormat("WaybillLanSourceHandler: обработка файла {0}", fileName);
						documentReader.ImportDocument(log, fileName);
						WaybillService.ParseWaybill(log);
					}
				}
				catch (Exception e) {
					var message = "Не удалось отформатировать документ.\nОшибка: " + e;
					_logger.ErrorFormat("WaybillLanSourceHandler: {0}, archfilename {1}, fileName {2}, error {3}", message, archFileName, fileName, e);
					DocumentReceiveLog.Log(supplierId, null, fileName, _currentDocumentType.DocType, message);
					return false;
				}
			}

			return true;
		}
	}
}