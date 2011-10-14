using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Castle.ActiveRecord;
using Common.MySql;
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
using FileHelper = Inforoom.Common.FileHelper;

namespace Inforoom.Downloader
{
	public class WaybillLANSourceHandler : BaseSourceHandler
	{
		private readonly InboundDocumentType[] _documentTypes;
		protected InboundDocumentType _currentDocumentType;

		public WaybillLANSourceHandler()
		{
			SourceType = "WAYBILLLAN";
			_documentTypes = new InboundDocumentType[] { new WaybillType(), new RejectType() };
		}

		// �������� ������ � ���������� �����������, ��������� �� ������� �������������� ������ �������
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
			//����� ����� ������� ����������
			DataRow drLanSource;
			// ��������� ������� � ������� � �����������.
			FillSourcesTable();

			while (dtSources.Rows.Count > 0)
			{
				try
				{
					_currentDocumentType = null; 
					// ����� ������� ������ (� ������� � ����������)
					drLanSource = dtSources.Rows[0];

					var documentReader = GetDocumentReader(drLanSource[WaybillSourcesTable.colReaderClassName].ToString());

					foreach(var documentType in _documentTypes)
						try
						{
							_currentDocumentType = documentType;

							// �������� ������ ������ �� �����
							var files = GetFileFromSource(documentReader);

							foreach (var sourceFileName in files)
							{
								GetCurrentFile(sourceFileName);

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
													_currentDocumentType.GetType().Name,
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
										if (!String.IsNullOrEmpty(sourceFileName) && File.Exists(sourceFileName))
											File.Delete(sourceFileName);
									}
									else
									{
										var supplierId = Convert.ToUInt32(drLanSource[WaybillSourcesTable.colFirmCode]);
										WriteLog(documentType.DocType, supplierId, null, Path.GetFileName(CurrFileName), 
											String.Format("�� ������� ����������� ���� '{0}'", Path.GetFileName(CurrFileName)));
										//����������� ���� �� �������, ������� ������� ��� �� �����
										if (!String.IsNullOrEmpty(sourceFileName) && File.Exists(sourceFileName))
											File.Delete(sourceFileName);
									}
									Cleanup();
								}

							}

						}
						catch (Exception typeException)
						{
							//������������ ������ � ������ ��������� ������ �� ����� ����������
							var Error = String.Format("�������� : {0}\n��� : {1}", dtSources.Rows[0][WaybillSourcesTable.colFirmCode], documentType.GetType().Name);
							Error += Environment.NewLine + Environment.NewLine + typeException;
							if (!typeException.ToString().Contains("����� ��������� � �������� ����������"))
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
					if (!ex.ToString().Contains("����� ��������� � �������� ����������"))
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
				// ���� � �����, �� ������� ����� �������� ���������
				// \FTPOptBox\<��� ���������>\Waybills\ (��� \Rejects\)
				pricePath = FileHelper.NormalizeDir(Settings.Default.FTPOptBoxPath) + 
					dtSources.Rows[0]["FirmCode"].ToString().PadLeft(3, '0') + 
					Path.DirectorySeparatorChar + _currentDocumentType.FolderName;
				// �������� ��� ����� �� ���� �����
				var ff = Directory.GetFiles(pricePath);
				// �������� ����� � ������������ �����������
				var newFiles = new List<string>();
				foreach (var newFileName in ff)
				{
					if (Array.Exists(documentReader.ExcludeExtentions,
						s => s.Equals(Path.GetExtension(newFileName), StringComparison.OrdinalIgnoreCase)))
					{
						// ���� ���� ���� � ������������ �����������, ������� ���
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
				LoggingToService(String.Format("�� ������� �������� ������ ������ ��� ����� {0}: {1}", 
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
				LoggingToService(String.Format("�� ������� ����������� ���� {0}({1}) : {2}", sourceFile, System.Runtime.InteropServices.Marshal.GetLastWin32Error(), ex));
			}
		}

		protected bool ProcessWaybillFile(string InFile, DataRow drCurrent, BaseDocumentReader documentReader)
		{
			//������ ������ 
			var Files = new[] { InFile };
			if (ArchiveHelper.IsArchive(InFile))
			{
				// �������� �����, ������������� �� ������
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
				var supplierId = Convert.ToUInt32(drCurrent[WaybillSourcesTable.colFirmCode]);
				WriteLog(_currentDocumentType.DocType, supplierId, null, Path.GetFileName(CurrFileName), 
					String.Format("�� ������� ��������� �����: {0}", exDivide));
				return false;
			}

			var processed = false;

			foreach (var file in Files)
			{
				if (MoveWaybill(InFile, file, drCurrent, documentReader))
					processed = true;
			}
			return processed;
		}

		protected bool MoveWaybill(string archFileName, string fileName, DataRow drCurrent, BaseDocumentReader documentReader)
		{
			using (var cleaner = new FileCleaner())
			{
				var supplierId = Convert.ToUInt32(drCurrent[WaybillSourcesTable.colFirmCode]);
				try
				{
					cleaner.Watch(fileName);

					var addresses = With.Connection(c => documentReader.GetClientCodes(c, supplierId, archFileName, fileName));
					var formatFile = documentReader.FormatOutputFile(fileName, drCurrent);
					cleaner.Watch(formatFile);

					foreach (var addressId in addresses)
					{
						var clientAddressId = (uint?) addressId;
						var clientId = GetClientIdByAddress(ref clientAddressId);
						if (clientId == null)
						{
							clientId = clientAddressId;
							clientAddressId = null;
						}

						var log = DocumentReceiveLog.LogNoCommit(supplierId,
							clientId,
							clientAddressId,
							formatFile,
							_currentDocumentType.DocType);

						_logger.InfoFormat("WaybillLANSourceHandler: ��������� ����� {0}", fileName);
						documentReader.ImportDocument(log, fileName);
						//log.CopyDocumentToClientDirectory();
						WaybillService.ParserDocument(log); 
					}
				}
				catch(Exception e)
				{
					var message = "�� ������� ��������������� ��������.\n������: " + e;										
					_logger.ErrorFormat("WaybillLANSourceHandler: {0}, archfilename {1}, fileName {2}, error {3}", message, archFileName, fileName, e);
					DocumentReceiveLog.Log(supplierId, null, null, fileName, _currentDocumentType.DocType, message);
					return false;
				}
			}

			return true;
		}

		private void WriteLog(DocType documentType, uint supplierId, uint? addressId, string logFileName, string comment)
		{
			var clientId = GetClientIdByAddress(ref addressId);
			if (clientId == null)
			{
				clientId = addressId;
				addressId = null;
			}

			DocumentReceiveLog.Log(supplierId, clientId, addressId, logFileName, documentType, comment);
		}

		private static BaseDocumentReader GetDocumentReader(string readerClassName)
		{ 
			Type result = null;
			var types = Assembly.GetExecutingAssembly()
								.GetModules()[0]
								.FindTypes(Module.FilterTypeNameIgnoreCase, readerClassName);
			if (types.Length > 1)
				throw new Exception(String.Format("������� ����� ������ ���� � ������ {0}", readerClassName));
			if (types.Length == 1)
				result = types[0];
			if (result == null)
				throw new Exception(String.Format("����� {0} �� ������", readerClassName));
			return (BaseDocumentReader)Activator.CreateInstance(result);
		}
	}
}
