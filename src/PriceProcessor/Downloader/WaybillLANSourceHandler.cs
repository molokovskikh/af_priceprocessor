using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Inforoom.PriceProcessor.Waybills;
using MySql.Data.MySqlClient;
using Inforoom.PriceProcessor.Properties;
using ExecuteTemplate;
using Inforoom.Downloader.DocumentReaders;
using Inforoom.Downloader.Documents;
using System.Net.Mail;
using System.Reflection;
using Inforoom.Common;

namespace Inforoom.Downloader
{
	public class ProcessedDocument
	{
		public uint DocumentLogId { get; set; }
		public string FormattedFilePath { get; set; }
		public string TempFilePath { get; set; }
	}

	public class WaybillLANSourceHandler : BaseSourceHandler
	{
		private readonly InboundDocumentType[] _documentTypes;
		private InboundDocumentType _currentDocumentType;

		public WaybillLANSourceHandler()
		{
			sourceType = "WAYBILLLAN";
			_documentTypes = new InboundDocumentType[] { new WaybillType(), new RejectType() };
		}

		// Выбирает данные о включенных поставщиках, накладные от которых обрабатываются особым образом
		protected override string GetSQLSources()
		{
			return @"
SELECT
  cd.FirmCode,
  cd.ShortName,
  st.EMailFrom,
  st.ReaderClassName
FROM
  usersettings.ClientsData AS cd
  INNER JOIN Documents.Waybill_Sources AS st ON CD.FirmCode = st.FirmCode
WHERE
cd.FirmStatus   = 1
and st.SourceID = 4";
		}

		protected override void ProcessData()
		{
			//набор строк похожих источников
			DataRow drLanSource;
			// Заполняем таблицу с данными о поставщиках.
			FillSourcesTable();

			while (dtSources.Rows.Count > 0)
			{
				try
				{
					_currentDocumentType = null; 
					// Берем нулевую строку (с данными о поставщике)
					drLanSource = dtSources.Rows[0];

					var documentReader = GetDocumentReader(drLanSource[WaybillSourcesTable.colReaderClassName].ToString());

					foreach(var documentType in _documentTypes)
						try
						{
							_currentDocumentType = documentType;

							// Получаем список файлов из папки
							var files = GetFileFromSource(documentReader);

							foreach (var sourceFileName in files)
							{
								GetCurrentFile(sourceFileName);

								if (!String.IsNullOrEmpty(CurrFileName))
								{
									var CorrectArchive = true;
									//Является ли скачанный файл корректным, если нет, то обрабатывать не будем
									if (ArchiveHelper.IsArchive(CurrFileName))
									{
										if (ArchiveHelper.TestArchive(CurrFileName))
										{
											try
											{
												PriceProcessor.FileHelper.ExtractFromArhive(CurrFileName, CurrFileName + ExtrDirSuffix);
											}
											catch (ArchiveHelper.ArchiveException)
											{
												CorrectArchive = false;
											}
										}
										else
											CorrectArchive = false;
									}

									if (CorrectArchive)
									{
										if (!ProcessWaybillFile(CurrFileName, drLanSource, documentReader))
										{
											using (var mm = new MailMessage(
												Settings.Default.FarmSystemEmail, 
												Settings.Default.DocumentFailMail,
												String.Format("{0} ({1})", drLanSource[WaybillSourcesTable.colShortName], SourceType),
												String.Format("Код поставщика : {0}\nФирма: {1}\nТип: {2}\nДата: {3}\nПричина: {4}",
													drLanSource[WaybillSourcesTable.colFirmCode],
													drLanSource[SourcesTableColumns.colShortName],
													_currentDocumentType.GetType().Name,
													DateTime.Now,
													"Не удалось сопоставить документ клиентам. Подробнее смотрите в таблице logs.document_logs.")))
											{
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
									else
									{
										var supplierId = Convert.ToInt32(drLanSource[WaybillSourcesTable.colFirmCode]);
										WriteLog(documentType.TypeID, supplierId, null, Path.GetFileName(CurrFileName), 
											String.Format("Не удалось распаковать файл '{0}'", Path.GetFileName(CurrFileName)));
										//Распаковать файл не удалось, поэтому удаляем его из папки
										if (!String.IsNullOrEmpty(sourceFileName) && File.Exists(sourceFileName))
											File.Delete(sourceFileName);
									}
									Cleanup();
								}

							}

						}
						catch (Exception typeException)
						{
							//Обрабатываем ошибку в случае обработки одного из типов документов
							var Error = String.Format("Источник : {0}\nТип : {1}", dtSources.Rows[0][WaybillSourcesTable.colFirmCode], documentType.GetType().Name);
							Error += Environment.NewLine + Environment.NewLine + typeException;
							LoggingToService(Error);
						}


					drLanSource.Delete();
					dtSources.AcceptChanges();
				}
				catch (Exception ex)
				{
					var error = String.Format("Источник : {0}", dtSources.Rows[0][WaybillSourcesTable.colFirmCode]);
					try
					{
						dtSources.Rows[0].Delete();
					}
					catch { }
					error += Environment.NewLine + Environment.NewLine + ex;
					LoggingToService(error);
					try
					{
						dtSources.AcceptChanges();
					}
					catch { }
				}
			}		
		}

		protected string[] GetFileFromSource(BaseDocumentReader documentReader)
		{
			var pricePath = String.Empty;
			try
			{
				// Путь к папке, из которой нужно забирать накладную
				// \FTPOptBox\<Код постащика>\Waybills\ (или \Rejects\)
				pricePath = FileHelper.NormalizeDir(Settings.Default.FTPOptBoxPath) + 
					dtSources.Rows[0]["FirmCode"].ToString().PadLeft(3, '0') + 
					Path.DirectorySeparatorChar + _currentDocumentType.FolderName;
				// Получаем все файлы из этой папки
				var ff = Directory.GetFiles(pricePath);
				// Отсекаем файлы с некорректным расширением
				var newFiles = new List<string>();
				foreach (var newFileName in ff)
				{
					if (Array.Exists(documentReader.ExcludeExtentions,
						s => s.Equals(Path.GetExtension(newFileName), StringComparison.OrdinalIgnoreCase)))
					{
						// Если есть файл с некорректным разрешением, удаляем его
						if (File.Exists(newFileName))
							File.Delete(newFileName);
					}
					else
						if (DateTime.Now.Subtract(File.GetLastWriteTime(newFileName)).TotalMinutes > Settings.Default.FileDownloadInterval)
							newFiles.Add(newFileName);
				}
				return documentReader.UnionFiles(newFiles.ToArray());
			}
			catch (Exception exDir)
			{
				LoggingToService(String.Format("Не удалось получить список файлов для папки {0}: {1}", 
					pricePath, exDir));
				return new string[] { };
			}
		}

		private void GetCurrentFile(string sourceFile)
		{
			CurrFileName = String.Empty;
			var NewFile = DownHandlerPath + Path.GetFileName(sourceFile);
			try
			{
				if (File.Exists(NewFile))
					File.Delete(NewFile);
				FileHelper.ClearReadOnly(sourceFile);
				File.Copy(sourceFile, NewFile);
				CurrFileName = NewFile;
			}
			catch (Exception ex)
			{
				LoggingToService(String.Format("Не удалось скопировать файл {0}({1}) : {2}", sourceFile, System.Runtime.InteropServices.Marshal.GetLastWin32Error(), ex));
			}
		}

		protected bool ProcessWaybillFile(string InFile, DataRow drCurrent, BaseDocumentReader documentReader)
		{
			//Массив файлов 
			var Files = new[] { InFile };
			if (ArchiveHelper.IsArchive(InFile))
			{
				// Получаем файлы, распакованные из архива
				Files = Directory.GetFiles(InFile + ExtrDirSuffix + Path.DirectorySeparatorChar, "*.*", SearchOption.AllDirectories);
			}

			if (!Directory.Exists(InFile + ExtrDirSuffix))
				Directory.CreateDirectory(InFile + ExtrDirSuffix);

			try
			{
				Files = documentReader.DivideFiles(InFile + ExtrDirSuffix + Path.DirectorySeparatorChar, Files);
			}
			catch (Exception exDivide)
			{
				var supplierId = Convert.ToInt32(drCurrent[WaybillSourcesTable.colFirmCode]);
				WriteLog(_currentDocumentType.TypeID, supplierId, null, Path.GetFileName(CurrFileName), 
					String.Format("Не удалось разделить файлы: {0}", exDivide.ToString()));
				return false;
			}

			//Если есть файлы для разбора, то хорошо, если нет, то архив не разобран
			var processed = Files.Length > 0;

			foreach (var s in Files)
			{
				var processedDocument = MoveWaybill(InFile, s, drCurrent, documentReader);
				if (processedDocument == null)
					processed = false;
				else
				{
					WaybillService.ParserDocument(Convert.ToUInt32(processedDocument.DocumentLogId), processedDocument.FormattedFilePath);
					if (File.Exists(processedDocument.FormattedFilePath))
						File.Delete(processedDocument.FormattedFilePath);
					if (File.Exists(processedDocument.TempFilePath))
						File.Delete(processedDocument.TempFilePath);
				}
			}
			return processed;
		}

		protected ProcessedDocument MoveWaybill(string ArchFileName, string FileName, DataRow drCurrent, BaseDocumentReader documentReader)
		{
			ProcessedDocument processedDocument = null;

			MethodTemplate.ExecuteMethod(
				new ExecuteArgs(),
				delegate(ExecuteArgs args)
				{
					//Пытаемся преобразовать имя файла 
					var _convertedFileName = FileHelper.FileNameToWindows1251(Path.GetFileName(FileName));
					if (!_convertedFileName.Equals(Path.GetFileName(FileName), StringComparison.CurrentCultureIgnoreCase))
					{
						//Если результат преобразования отличается от исходного имени, то переименовываем файл
						_convertedFileName = Path.GetDirectoryName(FileName) + Path.DirectorySeparatorChar + _convertedFileName;
						File.Move(FileName, _convertedFileName);
						FileName = _convertedFileName;
					}

					var cmdInsert = new MySqlCommand(@"
INSERT INTO logs.document_logs (FirmCode, ClientCode, AddressId, FileName, DocumentType, Addition) 
VALUES (?SupplierId, ?ClientId, ?AddressId, ?FileName, ?DocumentType, ?Addition); select last_insert_id();", _workConnection);
					cmdInsert.Parameters.AddWithValue("?SupplierId", drCurrent[WaybillSourcesTable.colFirmCode]);
					cmdInsert.Parameters.AddWithValue("?ClientId", DBNull.Value);
					cmdInsert.Parameters.AddWithValue("?AddressId", DBNull.Value);
					cmdInsert.Parameters.AddWithValue("?FileName", Path.GetFileName(FileName));
					cmdInsert.Parameters.AddWithValue("?Addition", DBNull.Value);
					cmdInsert.Parameters.AddWithValue("?DocumentType", _currentDocumentType.TypeID);

					List<ulong> listAddresses;

					cmdInsert.Transaction = args.DataAdapter.SelectCommand.Transaction;

					try
					{
						//Пытаемся получить список клиентов для накладной
						var supplierId = Convert.ToUInt64(drCurrent[WaybillSourcesTable.colFirmCode]);
						listAddresses = documentReader.GetClientCodes(_workConnection, supplierId, ArchFileName, FileName);
					}
					catch (Exception ex)
					{
						//Логируем и выходим
						cmdInsert.Parameters["?Addition"].Value = "Не удалось сопоставить документ клиентам.\nОшибка: " + ex;
						cmdInsert.ExecuteNonQuery();
						return false;
					}

					if (listAddresses != null)
					{
						string formatFile;
						try
						{
							//пытаемся отформатировать документ
							formatFile = documentReader.FormatOutputFile(FileName, drCurrent);
						}
						catch (Exception ex)
						{
							//Логируем и выходим
							var addressId = (int?)(listAddresses[0]);
							var clientId = GetClientIdByAddress(ref addressId);
							if (clientId == null)
							{
								clientId = addressId;
								addressId = null;
							}
							cmdInsert.Parameters["?ClientId"].Value = clientId;
							cmdInsert.Parameters["?AddressId"].Value = addressId;
							cmdInsert.Parameters["?Addition"].Value = "Не удалось отформатировать документ.\nОшибка: " + ex;
							cmdInsert.ExecuteNonQuery();
							return false;
						}

						foreach (var addressId in listAddresses)
						{
							var clientAddressId = (int?)addressId;
							var clientId = GetClientIdByAddress(ref clientAddressId);
							if (clientId == null)
							{
								clientId = clientAddressId;
								clientAddressId = null;
							}
							cmdInsert.Parameters["?ClientId"].Value = clientId;
							cmdInsert.Parameters["?AddressId"].Value = clientAddressId;
							cmdInsert.Parameters["?Addition"].Value = DBNull.Value;

							if (clientAddressId == null)
								clientAddressId = clientId;

							try
							{
								documentReader.ImportDocument(
									_workConnection,
									Convert.ToUInt64(drCurrent[WaybillSourcesTable.colFirmCode]),
									(ulong)clientAddressId,
									1,
									FileName);
							}
							catch (Exception ex)
							{
								cmdInsert.Parameters["?Addition"].Value = "Не удалось импортировать документ в базу.\nОшибка: " + ex;
								cmdInsert.ExecuteNonQuery();
								continue;
							}
							// Директория, куда будут складываться накладные и отказы для конкретного адреса
							var aptekaClientDirectory = FileHelper.NormalizeDir(Settings.Default.WaybillsPath) + 
								clientAddressId.ToString().PadLeft(3, '0') + Path.DirectorySeparatorChar + _currentDocumentType.FolderName;
							var outFileNameTemplate = aptekaClientDirectory + Path.DirectorySeparatorChar;

							if (!Directory.Exists(aptekaClientDirectory))
								Directory.CreateDirectory(aptekaClientDirectory);

							var documentLogId = cmdInsert.ExecuteScalar();							
							var outFileName = outFileNameTemplate + documentLogId + "_"
							                     + drCurrent["ShortName"]
							                     + "(" + Path.GetFileNameWithoutExtension(formatFile) + ")"
							                     + Path.GetExtension(formatFile);
							outFileName = PriceProcessor.FileHelper.NormalizeFileName(outFileName);
							//todo: filecopy здесь происходит логирование действий по копированию документов в папку клиента, из-за предположения, что есть проблема с пропажей документов
							if (File.Exists(outFileName))
								try
								{
									_logger.DebugFormat("MoveWaybill.Попытка удалить файл {0}", outFileName);
									File.Delete(outFileName);
									_logger.DebugFormat("MoveWaybill.Удаление файла успешно {0}", outFileName);
								}
								catch (Exception ex)
								{
									_logger.ErrorFormat("MoveWaybill.Ошибка при удалении файла {0}\r\n{1}", outFileName, ex);
								}

							File.Copy(formatFile, outFileName);
							_logger.InfoFormat("Файл {0} скопирован в документы клиента.", outFileName);
							// Сохраняем накладную в локальной папке
							SaveWaybill(clientAddressId, _currentDocumentType, outFileName);

							processedDocument = new ProcessedDocument {
								DocumentLogId = Convert.ToUInt32(documentLogId),
								FormattedFilePath = formatFile,
								TempFilePath = FileName,
							};
						}
					}
					return true;
				},
				false,
				_workConnection,
				true,
				false,
				(e, ex) => Ping());

			return processedDocument;
		}

		private void WriteLog(int? documentType, int? logSupplierId, int? logAddressId, string logFileName, string logAddition)
		{
			MethodTemplate.ExecuteMethod<ExecuteArgs, object>(new ExecuteArgs(), delegate(ExecuteArgs args)
			{
				var cmdInsert = new MySqlCommand(@"
INSERT INTO logs.document_logs (FirmCode, ClientCode, AddressId, FileName, Addition, DocumentType) 
VALUES (?SupplierId, ?ClientId, ?AddressId, ?FileName, ?Addition, ?DocumentType)", args.DataAdapter.SelectCommand.Connection);
				// Получаем идентификатор клиента по идентификатору адреса
				var logClientId = GetClientIdByAddress(ref logAddressId);
				if (logClientId == null)
				{
					logClientId = logAddressId;
					logAddressId = null;
				}

				cmdInsert.Parameters.AddWithValue("?SupplierId", logSupplierId);
				cmdInsert.Parameters.AddWithValue("?ClientId", logClientId);
				cmdInsert.Parameters.AddWithValue("?FileName", logFileName);
				cmdInsert.Parameters.AddWithValue("?Addition", logAddition);
				cmdInsert.Parameters.AddWithValue("?DocumentType", documentType);
				cmdInsert.Parameters.AddWithValue("?AddressId", logAddressId);
				cmdInsert.ExecuteNonQuery();

				return null;
			}, 
				null,
				_workConnection,
				true,
				false,
				delegate {
					Ping();
				});

		}

		private static BaseDocumentReader GetDocumentReader(string readerClassName)
		{ 
			Type result = null;
			var types = Assembly.GetExecutingAssembly()
								.GetModules()[0]
								.FindTypes(Module.FilterTypeNameIgnoreCase, readerClassName);
			if (types.Length > 1)
				throw new Exception(String.Format("Найдено более одного типа с именем {0}", readerClassName));
			if (types.Length == 1)
				result = types[0];
			if (result == null)
				throw new Exception(String.Format("Класс {0} не найден", readerClassName));
			return (BaseDocumentReader)Activator.CreateInstance(result);
		}
	}
}
