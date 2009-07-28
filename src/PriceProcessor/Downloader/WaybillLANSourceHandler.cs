using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
	class WaybillLANSourceHandler : BaseSourceHandler
	{
		private readonly InboundDocumentType[] _documentTypes;
		private InboundDocumentType _currentType;

		public WaybillLANSourceHandler()
		{
			sourceType = "WAYBILLLAN";
			_documentTypes = new InboundDocumentType[] { new WaybillType(), new RejectType() };
		}

		protected override string GetSQLSources()
		{
			return @"
SELECT
  cd.FirmCode,
  cd.ShortName,
  st.EMailFrom,
  st.ReaderClassName
FROM
  usersettings.ClientsData             as cd
  INNER JOIN Documents.Waybill_Sources AS st ON CD.FirmCode = st.FirmCode
WHERE
cd.FirmStatus   = 1
and st.SourceID = 4";
		}

		protected override void ProcessData()
		{
			//����� ����� ������� ����������
			DataRow drLanSource;
			FillSourcesTable();

			while (dtSources.Rows.Count > 0)
			{
				try
				{
					_currentType = null; 

					drLanSource = dtSources.Rows[0];

					var documentReader = GetDocumentReader(drLanSource[WaybillSourcesTable.colReaderClassName].ToString());


					foreach(var document in _documentTypes)
						try
						{
							_currentType = document;

							//�������� ������ ������ �� �����
							var files = GetFileFromSource(documentReader);

							foreach (var SourceFileName in files)
							{
								GetCurrentFile(SourceFileName);

								if (!String.IsNullOrEmpty(CurrFileName))
								{
									var CorrectArchive = true;
									//�������� �� ��������� ���� ����������, ���� ���, �� ������������ �� �����
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
												String.Format("��� ���������� : {0}\n�����: {1}\n���: {2}\n����: {3}\n�������: {4}",
													drLanSource[WaybillSourcesTable.colFirmCode],
													drLanSource[SourcesTableColumns.colShortName],
													_currentType.GetType().Name,
													DateTime.Now,
													"�� ������� ����������� �������� ��������. ��������� �������� � ������� logs.document_logs.")))
											{
												if (!String.IsNullOrEmpty(CurrFileName))
													mm.Attachments.Add(new Attachment(CurrFileName));
												var sc = new SmtpClient(Settings.Default.SMTPHost);
												sc.Send(mm);
											}
										}
										//����� ��������� ����� ������� ��� �� �����
										if (!String.IsNullOrEmpty(SourceFileName) && File.Exists(SourceFileName))
											File.Delete(SourceFileName);
									}
									else
									{
										WriteLog(document.TypeID, Convert.ToInt32(drLanSource[WaybillSourcesTable.colFirmCode]), null, Path.GetFileName(CurrFileName), String.Format("�� ������� ����������� ���� '{0}'", Path.GetFileName(CurrFileName)));
										//����������� ���� �� �������, ������� ������� ��� �� �����
										if (!String.IsNullOrEmpty(SourceFileName) && File.Exists(SourceFileName))
											File.Delete(SourceFileName);
									}
									DeleteCurrFile();
								}

							}

						}
						catch (Exception typeException)
						{
							//������������ ������ � ������ ��������� ������ �� ����� ����������
							var Error = String.Format("�������� : {0}\n��� : {1}", dtSources.Rows[0][WaybillSourcesTable.colFirmCode], document.GetType().Name);
							Error += Environment.NewLine + Environment.NewLine + typeException;
							LoggingToService(Error);
						}


					drLanSource.Delete();
					dtSources.AcceptChanges();
				}
				catch (Exception ex)
				{
					var error = String.Format("�������� : {0}", dtSources.Rows[0][WaybillSourcesTable.colFirmCode]);
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
			var PricePath = String.Empty;
			try
			{
				PricePath = FileHelper.NormalizeDir(Settings.Default.FTPOptBoxPath) + dtSources.Rows[0]["FirmCode"].ToString().PadLeft(3, '0') + Path.DirectorySeparatorChar + _currentType.FolderName;
				var ff = Directory.GetFiles(PricePath);

				//�������� ����� � ������������ �����������
				var newFiles = new List<string>();
				foreach (var newFileName in ff)
				{
					if (Array.Exists(documentReader.ExcludeExtentions,
					                 s => s.Equals(Path.GetExtension(newFileName), StringComparison.OrdinalIgnoreCase)))
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
				LoggingToService(String.Format("�� ������� �������� ������ ������ ��� ����� {0}: {1}", PricePath, exDir));
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
				LoggingToService(String.Format("�� ������� ����������� ���� {0}({1}) : {2}", sourceFile, System.Runtime.InteropServices.Marshal.GetLastWin32Error(), ex));
			}
		}

		protected bool ProcessWaybillFile(string InFile, DataRow drCurrent, BaseDocumentReader documentReader)
		{
			//������ ������ 
			var Files = new[] { InFile };
			if (ArchiveHelper.IsArchive(InFile))
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
				WriteLog(_currentType.TypeID, Convert.ToInt32(drCurrent[WaybillSourcesTable.colFirmCode]), null, Path.GetFileName(CurrFileName), String.Format("�� ������� ��������� �����: {0}", exDivide.ToString()));
				return false;
			}

			//���� ���� ����� ��� �������, �� ������, ���� ���, �� ����� �� ��������
			var processed = Files.Length > 0;

			foreach (var s in Files)
			{
				if (!MoveWaybill(InFile, s, drCurrent, documentReader))
					processed = false;
			}
			return processed;
		}

		protected bool MoveWaybill(string ArchFileName, string FileName, DataRow drCurrent, BaseDocumentReader documentReader)
		{
			return MethodTemplate.ExecuteMethod(
				new ExecuteArgs(),
				delegate(ExecuteArgs args)
				{
					//�������� ������������� ��� ����� 
					var _convertedFileName = FileHelper.FileNameToWindows1251(Path.GetFileName(FileName));
					if (!_convertedFileName.Equals(Path.GetFileName(FileName), StringComparison.CurrentCultureIgnoreCase))
					{
						//���� ��������� �������������� ���������� �� ��������� �����, �� ��������������� ����
						_convertedFileName = Path.GetDirectoryName(FileName) + Path.DirectorySeparatorChar + _convertedFileName;
						File.Move(FileName, _convertedFileName);
						FileName = _convertedFileName;
					}

					var cmdInsert = new MySqlCommand("insert into logs.document_logs (FirmCode, ClientCode, FileName, DocumentType, Addition) values (?FirmCode, ?ClientCode, ?FileName, ?DocumentType, ?Addition); select last_insert_id();", _workConnection);
					cmdInsert.Parameters.AddWithValue("?FirmCode", drCurrent[WaybillSourcesTable.colFirmCode]);
					cmdInsert.Parameters.AddWithValue("?ClientCode", DBNull.Value);
					cmdInsert.Parameters.AddWithValue("?FileName", Path.GetFileName(FileName));
					cmdInsert.Parameters.AddWithValue("?Addition", DBNull.Value);
					cmdInsert.Parameters.AddWithValue("?DocumentType", _currentType.TypeID);

					List<ulong> listClients;

					cmdInsert.Transaction = args.DataAdapter.SelectCommand.Transaction;

					try
					{
						//�������� �������� ������ �������� ��� ���������
						listClients = documentReader.GetClientCodes(_workConnection, Convert.ToUInt64(drCurrent[WaybillSourcesTable.colFirmCode]), ArchFileName, FileName);
					}
					catch (Exception ex)
					{
						//�������� � �������
						cmdInsert.Parameters["?Addition"].Value = "�� ������� ����������� �������� ��������.\n������: " + ex;
						cmdInsert.ExecuteNonQuery();
						return false;
					}

					if (listClients != null)
					{
						string formatFile;
						try
						{
							//�������� ��������������� ��������
							formatFile = documentReader.FormatOutputFile(FileName, drCurrent);
						}
						catch (Exception ex)
						{
							//�������� � �������
							cmdInsert.Parameters["?ClientCode"].Value = listClients[0];
							cmdInsert.Parameters["?Addition"].Value = "�� ������� ��������������� ��������.\n������: " + ex;
							cmdInsert.ExecuteNonQuery();
							return false;
						}

						foreach (var AptekaClientCode in listClients)
						{
							cmdInsert.Parameters["?ClientCode"].Value = AptekaClientCode;
							cmdInsert.Parameters["?Addition"].Value = DBNull.Value;

							try
							{
								documentReader.ImportDocument(
									_workConnection,
									Convert.ToUInt64(drCurrent[WaybillSourcesTable.colFirmCode]),
									AptekaClientCode,
									1,
									FileName);
							}
							catch (Exception ex)
							{
								cmdInsert.Parameters["?Addition"].Value = "�� ������� ������������� �������� � ����.\n������: " + ex;
								cmdInsert.ExecuteNonQuery();
								continue;
							}
							var AptekaClientDirectory = FileHelper.NormalizeDir(Settings.Default.FTPOptBoxPath) + AptekaClientCode.ToString().PadLeft(3, '0') + Path.DirectorySeparatorChar + _currentType.FolderName;
							var OutFileNameTemplate = AptekaClientDirectory + Path.DirectorySeparatorChar;

							if (!Directory.Exists(AptekaClientDirectory))
								Directory.CreateDirectory(AptekaClientDirectory);


							var OutFileName = OutFileNameTemplate + cmdInsert.ExecuteScalar() + "_"
							                     + drCurrent["ShortName"]
							                     + "(" + Path.GetFileNameWithoutExtension(formatFile) + ")"
							                     + Path.GetExtension(formatFile);
							OutFileName = PriceProcessor.FileHelper.NormalizeFileName(OutFileName);

							//todo: filecopy ����� ���������� ����������� �������� �� ����������� ���������� � ����� �������, ��-�� �������������, ��� ���� �������� � �������� ����������
							if (File.Exists(OutFileName))
								try
								{
									_logger.DebugFormat("MoveWaybill.������� ������� ���� {0}", OutFileName);
									File.Delete(OutFileName);
									_logger.DebugFormat("MoveWaybill.�������� ����� ������� {0}", OutFileName);
								}
								catch (Exception ex)
								{
									_logger.ErrorFormat("MoveWaybill.������ ��� �������� ����� {0}\r\n{1}", OutFileName, ex);
								}

							File.Copy(formatFile, OutFileName);
							_logger.InfoFormat("���� {0} ���������� � ��������� �������.", OutFileName);
						}

						if (File.Exists(formatFile))
							File.Delete(formatFile);
						if (File.Exists(FileName))
							File.Delete(FileName);

					}

					return true;
				},
				false,
				_workConnection,
				true,
				false,
				(e, ex) => Ping());
		}

		private void WriteLog(int? DocumentType, int? logFirmCode, int? logClientCode, string logFileName, string logAddition)
		{
			MethodTemplate.ExecuteMethod<ExecuteArgs, object>(new ExecuteArgs(), delegate(ExecuteArgs args)
			{
				var cmdInsert = new MySqlCommand("insert into logs.document_logs (FirmCode, ClientCode, FileName, Addition, DocumentType) values (?FirmCode, ?ClientCode, ?FileName, ?Addition, ?DocumentType)", args.DataAdapter.SelectCommand.Connection);

				cmdInsert.Parameters.AddWithValue("?FirmCode", logFirmCode);
				cmdInsert.Parameters.AddWithValue("?ClientCode", logClientCode);
				cmdInsert.Parameters.AddWithValue("?FileName", logFileName);
				cmdInsert.Parameters.AddWithValue("?Addition", logAddition);
				cmdInsert.Parameters.AddWithValue("?DocumentType", DocumentType);
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

		private static BaseDocumentReader GetDocumentReader(string ReaderClassName)
		{ 
			Type result = null;
			var types = Assembly.GetExecutingAssembly().GetModules()[0].FindTypes(Module.FilterTypeNameIgnoreCase, ReaderClassName);
			if (types.Length > 1)
				throw new Exception(String.Format("������� ����� ������ ���� � ������ {0}", ReaderClassName));
			if (types.Length == 1)
				result = types[0];
			if (result == null)
				throw new Exception(String.Format("����� {0} �� ������", ReaderClassName));
			return (BaseDocumentReader)Activator.CreateInstance(result);
		}

	}
}
