using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.IO;
using Inforoom.Formalizer;
using MySql.Data.MySqlClient;
using Inforoom.Downloader.Properties;
using ExecuteTemplate;
using Inforoom.Downloader.DocumentReaders;
using Inforoom.Downloader.Documents;
using System.Net.Mail;
using System.Reflection;

namespace Inforoom.Downloader
{
	class WaybillLANSourceHandler : BaseSourceHandler
	{
		public WaybillLANSourceHandler()
			: base()
		{
			this.sourceType = "WAYBILLLAN";
		}

		protected override string GetSQLSources()
		{
			return String.Format(@"
SELECT
  cd.FirmCode,
  cd.ShortName,
  st.EMailFrom,
  st.ReaderClassName
FROM
           {1}             as st
INNER JOIN {0} AS CD ON CD.FirmCode = st.FirmCode
WHERE
cd.FirmStatus   = 1
and st.SourceID = 4",
				Settings.Default.tbClientsData,
				Settings.Default.tbWaybillSources);
		}

		protected override void ProcessData()
		{
			//набор строк похожих источников
			DataRow drLanSource;
			string[] files;
			FillSourcesTable();
			BaseDocumentReader documentReader;

			while (dtSources.Rows.Count > 0)
			{
				drLanSource = null;
				try
				{
					drLanSource = dtSources.Rows[0];

					documentReader = GetDocumentReader(drLanSource[WaybillSourcesTable.colReaderClassName].ToString());

					//Получаем список файлов из папки
					files = GetFileFromSource(documentReader);

					foreach (string SourceFileName in files)
					{
						GetCurrentFile(SourceFileName);

						if (!String.IsNullOrEmpty(CurrFileName))
						{
							bool CorrectArchive = true;
							//Является ли скачанный файл корректным, если нет, то обрабатывать не будем
							if (ArchiveHlp.IsArchive(CurrFileName))
							{
								if (ArchiveHlp.TestArchive(CurrFileName))
								{
									try
									{
										ExtractFromArhive(CurrFileName, CurrFileName + ExtrDirSuffix);
									}
									catch (ArchiveHlp.ArchiveException)
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
									MailMessage mm = new MailMessage(Settings.Default.SMTPUserError, Settings.Default.DocumentFailMail,
										String.Format("{1} ({2})", CurrPriceCode, drLanSource[WaybillSourcesTable.colShortName], SourceType),
										String.Format("Код поставщика : {0}\nФирма: {1}\nДата: {2}\nПричина: {3}",
											drLanSource[WaybillSourcesTable.colFirmCode], 
											drLanSource[SourcesTable.colShortName], 
											DateTime.Now,
											"Не удалось сопоставить документ клиентам. Подробнее смотрите в таблице logs.document_logs."));
									if (!String.IsNullOrEmpty(CurrFileName))
										mm.Attachments.Add(new Attachment(CurrFileName));
									SmtpClient sc = new SmtpClient(Settings.Default.SMTPHost);
									sc.Send(mm);
								}
								//После обработки файла удаляем его из папки
								if (!String.IsNullOrEmpty(SourceFileName) && File.Exists(SourceFileName))
									File.Delete(SourceFileName);
							}
							else
							{
								WriteLog(Convert.ToInt32(drLanSource[WaybillSourcesTable.colFirmCode]), null, Path.GetFileName(CurrFileName), String.Format("Не удалось распаковать файл '{0}'", Path.GetFileName(CurrFileName)));
								//Распаковать файл не удалось, поэтому удаляем его из папки
								if (!String.IsNullOrEmpty(SourceFileName) && File.Exists(SourceFileName))
									File.Delete(SourceFileName);
							}
							DeleteCurrFile();
						}

					}

					drLanSource.Delete();
					dtSources.AcceptChanges();
				}
				catch (Exception ex)
				{
					string Error = String.Empty;
					Error = String.Format("Источник : {0}", dtSources.Rows[0][WaybillSourcesTable.colFirmCode]);
					try
					{
						dtSources.Rows[0].Delete();
					}
					catch { }
					Error += Environment.NewLine + Environment.NewLine + ex.ToString();
					LoggingToService(Error);
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
			string PricePath = String.Empty;
			try
			{
				PricePath = NormalizeDir(Settings.Default.FTPOptBox) + dtSources.Rows[0]["FirmCode"].ToString().PadLeft(3, '0') + Path.DirectorySeparatorChar + "Waybills";
				string[] ff = Directory.GetFiles(PricePath);

				//Отсекаем файлы с некорректным расширением
				List<string> newFiles = new List<string>();
				foreach (string newFileName in ff)
				{
					if (Array.Exists<string>(documentReader.ExcludeExtentions, delegate(string s) { return s.Equals(Path.GetExtension(newFileName), StringComparison.OrdinalIgnoreCase); }))
					{
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
				LoggingToService(String.Format("Не удалось получить список файлов для папки {0}: {1}", PricePath, exDir));
				return new string[] { };
			}
		}

		private void GetCurrentFile(string sourceFile)
		{
			CurrFileName = String.Empty;
			string NewFile = DownHandlerPath + Path.GetFileName(sourceFile);
			try
			{
				if (File.Exists(NewFile))
					File.Delete(NewFile);
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
			bool processed;
			//Массив файлов 
			string[] Files = new string[] { InFile };
			if (ArchiveHlp.IsArchive(InFile))
			{
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
				processed = false;
				WriteLog(Convert.ToInt32(drCurrent[WaybillSourcesTable.colFirmCode]), null, Path.GetFileName(CurrFileName), String.Format("Не удалось разделить файлы: {0}", exDivide.ToString()));
				return processed;
			}

			//Если есть файлы для разбора, то хорошо, если нет, то архив не разобран
			processed = Files.Length > 0;

			foreach (string s in Files)
			{
				if (!MoveWaybill(InFile, s, drCurrent, documentReader))
					processed = false;
			}
			return processed;
		}

		protected bool MoveWaybill(string ArchFileName, string FileName, DataRow drCurrent, BaseDocumentReader documentReader)
		{
			return MethodTemplate.ExecuteMethod<ExecuteArgs, bool>(
				new ExecuteArgs(),
				delegate(ExecuteArgs args)
				{
					MySqlCommand cmdInsert = new MySqlCommand("insert into logs.document_logs (FirmCode, ClientCode, FileName, DocumentType, Addition) values (?FirmCode, ?ClientCode, ?FileName, 1, ?Addition); select last_insert_id();", cWork);
					cmdInsert.Parameters.AddWithValue("?FirmCode", drCurrent[WaybillSourcesTable.colFirmCode]);
					cmdInsert.Parameters.AddWithValue("?ClientCode", DBNull.Value);
					cmdInsert.Parameters.AddWithValue("?FileName", Path.GetFileName(FileName));
					cmdInsert.Parameters.AddWithValue("?Addition", DBNull.Value);

					List<ulong> listClients = null;
					string AptekaClientDirectory;
					string OutFileNameTemplate;
					string OutFileName;
					string formatFile;

					cmdInsert.Transaction = args.DataAdapter.SelectCommand.Transaction;

					try
					{
						//Пытаемся получить список клиентов для накладной
						listClients = documentReader.GetClientCodes(cWork, Convert.ToUInt64(drCurrent[WaybillSourcesTable.colFirmCode]), ArchFileName, FileName);
					}
					catch (Exception ex)
					{
						//Логируем и выходим
						cmdInsert.Parameters["?Addition"].Value = "Не удалось сопоставить документ клиентам.\nОшибка: " + ex.ToString();
						cmdInsert.ExecuteNonQuery();
						return false;
					}

					if (listClients != null)
					{
						try
						{
							//пытаемся отформатировать документ
							formatFile = documentReader.FormatOutputFile(FileName, drCurrent);
						}
						catch (Exception ex)
						{
							//Логируем и выходим
							cmdInsert.Parameters["?ClientCode"].Value = listClients[0];
							cmdInsert.Parameters["?Addition"].Value = "Не удалось отформатировать документ.\nОшибка: " + ex.ToString();
							cmdInsert.ExecuteNonQuery();
							return false;
						}

						foreach (ulong AptekaClientCode in listClients)
						{
							cmdInsert.Parameters["?ClientCode"].Value = AptekaClientCode;
							cmdInsert.Parameters["?Addition"].Value = DBNull.Value;

							try
							{
								documentReader.ImportDocument(
									cWork,
									Convert.ToUInt64(drCurrent[WaybillSourcesTable.colFirmCode]),
									AptekaClientCode,
									1,
									FileName);
							}
							catch (Exception ex)
							{
								cmdInsert.Parameters["?Addition"].Value = "Не удалось импортировать документ в базу.\nОшибка: " + ex.ToString();
								cmdInsert.ExecuteNonQuery();
								continue;
							}
							AptekaClientDirectory = NormalizeDir(Settings.Default.FTPOptBox) + AptekaClientCode.ToString().PadLeft(3, '0') + Path.DirectorySeparatorChar + "Waybills";
							OutFileNameTemplate = AptekaClientDirectory + Path.DirectorySeparatorChar;
							OutFileName = String.Empty;

							if (!Directory.Exists(AptekaClientDirectory))
								Directory.CreateDirectory(AptekaClientDirectory);


							OutFileName = OutFileNameTemplate + cmdInsert.ExecuteScalar().ToString() + "_"
								+ drCurrent["ShortName"].ToString()
								+ "(" + Path.GetFileNameWithoutExtension(formatFile) + ")"
								+ Path.GetExtension(formatFile);
							OutFileName = NormalizeFileName(OutFileName);

							if (File.Exists(OutFileName))
								try
								{
									File.Delete(OutFileName);
								}
								catch { }

							File.Copy(formatFile, OutFileName);
						}

						if (File.Exists(formatFile))
							File.Delete(formatFile);
						if (File.Exists(FileName))
							File.Delete(FileName);

					}

					return true;
				},
				false,
				cWork,
				true,
				null,
				false,
				delegate(ExecuteArgs args, MySqlException ex)
				{
					Ping();
				});
		}

		private void WriteLog(int? logFirmCode, int? logClientCode, string logFileName, string logAddition)
		{
			MethodTemplate.ExecuteMethod<ExecuteArgs, object>(new ExecuteArgs(), delegate(ExecuteArgs args)
			{
				MySqlCommand cmdInsert = new MySqlCommand("insert into logs.document_logs (FirmCode, ClientCode, FileName, Addition, DocumentType) values (?FirmCode, ?ClientCode, ?FileName, ?Addition, 1)", args.DataAdapter.SelectCommand.Connection);

				cmdInsert.Parameters.AddWithValue("?FirmCode", logFirmCode);
				cmdInsert.Parameters.AddWithValue("?ClientCode", logClientCode);
				cmdInsert.Parameters.AddWithValue("?FileName", logFileName);
				cmdInsert.Parameters.AddWithValue("?Addition", logAddition);
				cmdInsert.ExecuteNonQuery();

				return null;
			}, 
				null,
				cWork,
				true,
				null,
				false,
				delegate(ExecuteArgs args, MySqlException ex)
				{
					Ping();
				});

		}

		private BaseDocumentReader GetDocumentReader(string ReaderClassName)
		{ 
			Type result = null;
			Type[] types = Assembly.GetExecutingAssembly().GetModules()[0].FindTypes(Module.FilterTypeNameIgnoreCase, ReaderClassName);
			if (types.Length > 1)
				throw new Exception(String.Format("Найдено более одного типа с именем {0}", ReaderClassName));
			if (types.Length == 1)
				result = types[0];
			if (result == null)
				throw new Exception(String.Format("Класс {0} не найден", ReaderClassName));
			return (BaseDocumentReader)Activator.CreateInstance(result);
		}

	}
}
