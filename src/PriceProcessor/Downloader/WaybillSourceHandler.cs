using System;
using System.Collections.Generic;
using System.Data;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.Mime;
using Inforoom.PriceProcessor.Properties;
using MySql.Data.MySqlClient;
using System.IO;
using ExecuteTemplate;
using Inforoom.Downloader.Documents;
using Inforoom.Common;


namespace Inforoom.Downloader
{

	public class WaybillSourceHandler : EMAILSourceHandler
	{
		// ��� ������� (������)
		private int? _aptekaClientCode;

		// ���� ���������� (���������, ������)
		private readonly List<InboundDocumentType> _documentTypes;

		// ��� �������� ��������� (��������� ��� �����)
		private InboundDocumentType _currentDocumentType;

		// Email, �� �������� ����� ������� ��������� � ������
		private string _imapUser = Settings.Default.WaybillIMAPUser;

		// ������ ��� ���������� email-�
		private string _imapPassword = Settings.Default.WaybillIMAPPass;

		public WaybillSourceHandler()
		{
			sourceType = "WAYBILL";
			_documentTypes = new List<InboundDocumentType> { 
				new WaybillType(), new RejectType() 
			};
		}

		public WaybillSourceHandler(string imapUser, string imapPassword)
			: this()
		{
			if (!String.IsNullOrEmpty(imapUser) && !String.IsNullOrEmpty(imapPassword))
			{
				_imapUser = imapUser;
				_imapPassword = imapPassword;
			}
		}

		protected override void IMAPAuth(IMAP_Client client)
		{
			client.Authenticate(_imapUser, _imapPassword);
		}

		protected override bool CheckMime(Mime m, ref string causeSubject, 
			ref string causeBody, ref string systemError)
		{
			var emailList = String.Empty;
			_aptekaClientCode = null;
			_currentDocumentType = null;

			// �������� ���-�� ���������� �������, �.�. ������������ 
			// �� @waybills.analit.net ��� �� @refused.analit.net
			var correctAddresCount = CorrectClientAddress(m.MainEntity.To, ref emailList);
			// ��� ������, ���� ���-�� �������� ������ 0 � ��������� ������ ���� ����� ��� ����������
			bool res = (m.Attachments.Length > 0) && (correctAddresCount == 1);
			// ���� �� ����������� � ��������
			if (correctAddresCount == 0)
			{
				systemError = "�� ������ ������.";
				causeSubject = Settings.Default.ResponseDocSubjectTemplateOnNonExistentClient;
				causeBody = Settings.Default.ResponseDocBodyTemplateOnNonExistentClient;
			}
			else
				// ���� ��� ��������
				if ((correctAddresCount == 1) && (m.Attachments.Length == 0))
				{
					systemError = "������ �� �������� ��������.";
					causeSubject = Settings.Default.ResponseDocSubjectTemplateOnNothingAttachs;
					causeBody = Settings.Default.ResponseDocBodyTemplateOnNothingAttachs;
				}
				else
					// ���� ��������� �������� � ������ �����������
					if (correctAddresCount > 1)
					{
						systemError = "������ ����������� ���������� ��������.";
						causeSubject = Settings.Default.ResponseDocSubjectTemplateOnMultiDomen;
						causeBody = Settings.Default.ResponseDocBodyTemplateOnMultiDomen;
					}
					else
						if (m.Attachments.Length > 0)
						{ 
							bool attachmentsIsBigger = false;
							foreach(var attachment in m.Attachments)
								if ((attachment.Data.Length / 1024.0) > Settings.Default.MaxWaybillAttachmentSize)
								{
									attachmentsIsBigger = true;
									break;
								}
							if (attachmentsIsBigger)
							{
								res = false;

								systemError = String.Format("������ �������� �������� �������� ������ ����������� ����������� �������� ({0} ��).", 
									Settings.Default.MaxWaybillAttachmentSize);
								causeSubject = Settings.Default.ResponseDocSubjectTemplateOnMaxWaybillAttachment;
								causeBody = String.Format(Settings.Default.ResponseDocBodyTemplateOnMaxWaybillAttachment, Settings.Default.MaxWaybillAttachmentSize);
							}
						}
			return res;
		}

		/// <summary>
		/// ���������, ���������� �� ������ � ��������� �����.
		/// ����� ���� ��������� ��� ����� ������� � ������� future.Addresses,
		/// ������� ����� �������, ��� ����� ��������� ����� ������� �� �������������
		/// </summary>
		private bool ClientExists(int checkClientCode)
		{
			var queryGetClientCode = String.Format(@"
SELECT cd.FirmCode 
FROM usersettings.ClientsData cd
WHERE cd.FirmType = 1 AND FirmCode = {0}
UNION
SELECT Addr.Id
FROM Future.Addresses Addr
WHERE Addr.Id = {0} OR Addr.LegacyId = {0}
", checkClientCode);

			return MethodTemplate.ExecuteMethod(
				new ExecuteArgs(), 
				delegate {
					var clientCode = MySqlHelper.ExecuteScalar(_workConnection, queryGetClientCode);
					return (clientCode != null);
				},
				false,
				_workConnection,
				true,
				false,
				delegate { Ping(); });
		}

		/// <summary>
		/// ��������� ��� ������� (��� ��� ������ �������) �� email ������,
		/// �� ������� ��������� �������� ��������� (��� �����)
		/// </summary>
		/// <returns>���� ��� �������� � ������������� ���� ������� 
		/// (��� ���� ������), ����� ��������� ���� ���. 
		/// ���� ��� �� ������� ������� ��� �� �� ������ �� ����� ����� ��������,
		/// �� ����� ����� �������, ����� ��������� null</returns>
		private int? GetClientCode(string emailAddress)
		{ 
			emailAddress = emailAddress.ToLower();
			InboundDocumentType testType = null;
			int? testClientCode = null;

			foreach (var documentType in _documentTypes)
			{
				int clientCode;

				// �������� ������� ��� ������� �� email ������
				if (documentType.ParseEmail(emailAddress, out clientCode))
				{
					testClientCode = clientCode;
					testType = documentType;
					break;
				}
			}

			if (testType != null)
			{
				if (ClientExists(testClientCode.Value))
				{
					if (_currentDocumentType == null)
					{
						_currentDocumentType = testType;
						_aptekaClientCode = testClientCode;
					}
				}
				else
					testClientCode = null;
			}

			return testClientCode;
		}

		private int CorrectClientAddress(AddressList addressList, ref string emailList)
		{
			int? currentClientCode;
			int clientCodeCount = 0;

			// ����������� �� ���� ������� TO � ���� ����� ���� 
			// <\d+@waybills.analit.net> ��� <\d+@refused.analit.net>
			// ���� ����� ������� ���������, �� �������, ��� ������ ��������� � �� ��������� ��� ������
			foreach(var mailbox in  addressList.Mailboxes)
			{
				currentClientCode = GetClientCode(GetCorrectEmailAddress(mailbox.EmailAddress));
				if (currentClientCode.HasValue)
				{
					if (!String.IsNullOrEmpty(emailList))
						emailList += Environment.NewLine;
					emailList += GetCorrectEmailAddress(mailbox.EmailAddress);
					clientCodeCount++;
				}
			}
			return clientCodeCount;
		}

		/// <summary>
		/// ���������� SQL ������ ��� ������� ����������� � e-mail-��, 
		/// � ������� ��� ����� ���������� ��������� � ������ (��� ���������� ������)
		/// </summary>
		protected override string GetSQLSources()
		{
			return @"
SELECT
  cd.FirmCode,
  cd.ShortName,
  r.Region as RegionName,
  st.EMailFrom
FROM
	Documents.Waybill_Sources AS st
	INNER JOIN usersettings.ClientsData AS cd ON CD.FirmCode = st.FirmCode
	INNER JOIN farm.regions AS r ON r.RegionCode = cd.RegionCode
WHERE
cd.FirmStatus = 1
AND st.SourceID = 1
";
/*			return @"
SELECT
  cd.FirmCode,
  cd.ShortName,
  r.Region as RegionName,
  st.EMailFrom
FROM
	usersettings.ClientsData AS Apteka,
	Documents.Waybill_Sources AS st
	INNER JOIN usersettings.ClientsData AS cd ON CD.FirmCode = st.FirmCode
	INNER JOIN farm.regions AS r ON r.RegionCode = cd.RegionCode
WHERE
	cd.FirmStatus = 1
	AND (Apteka.FirmCode = ?AptekaClientCode)
	AND st.SourceID = 1";*/
		}

		protected override DataTable GetSourcesTable(ExecuteArgs e)
		{
			dtSources.Clear();
			daFillSources.SelectCommand.Transaction = e.DataAdapter.SelectCommand.Transaction;
			daFillSources.SelectCommand.Parameters.Clear();
			//daFillSources.SelectCommand.Parameters.AddWithValue("?AptekaClientCode", _aptekaClientCode);
			daFillSources.Fill(dtSources);
			return dtSources;
		}

		protected override void ErrorOnCheckMime(Mime m, AddressList FromList, 
			string AttachNames, string causeSubject, string causeBody, string systemError)
		{
			if (causeBody != String.Empty)
			{
				SendErrorLetterToProvider(FromList, causeSubject, causeBody, m);
				WriteLog(
					(_currentDocumentType != null) ? (int?)_currentDocumentType.TypeID : null,
					GetFirmCodeByFromList(FromList), 
					_aptekaClientCode, 
					null, 
					String.Format(@"{0}
�����������            : {1}
����������             : {2}
������ ��������        : 
{3}
���� ������ ���������� : {4}
���� ������ ���������� : 
{5}", 
							 systemError, 
							 FromList.ToAddressListString(), 
							 m.MainEntity.To.ToAddressListString(), 
							 AttachNames, 
							 causeSubject, 
							 causeBody), 
					currentUID);
			}
			else
				SendUnrecLetter(m, FromList, AttachNames, "�� ������������ ������.");
		}

		protected override void ErrorOnProcessAttachs(Mime m, AddressList FromList, 
			string AttachNames, string causeSubject, string causeBody)
		{
			try
			{
				const string cause = "��� ������� E-mail �� ������ �������� � ������� documents.waybill_sources";
				var ms = new MemoryStream(m.ToByteData());
				SendErrorLetterToProvider(
					FromList, 
					Settings.Default.ResponseDocSubjectTemplateOnUnknownProvider, 
					Settings.Default.ResponseDocBodyTemplateOnUnknownProvider, m);
				FailMailSend(m.MainEntity.Subject, FromList.ToAddressListString(), 
					m.MainEntity.To.ToAddressListString(), m.MainEntity.Date, ms, AttachNames, cause);

				WriteLog((_currentDocumentType != null) ? (int?)_currentDocumentType.TypeID : null,
					GetFirmCodeByFromList(FromList), _aptekaClientCode, null,
					String.Format(@"{0} 
�����������     : {1}
����������      : {2}
������ �������� : 
{3}
���� ������ ���������� : {4}
���� ������ ���������� : 
{5}",
						cause,
						FromList.ToAddressListString(),
						m.MainEntity.To.ToAddressListString(),
						AttachNames,
						Settings.Default.ResponseDocSubjectTemplateOnUnknownProvider, 
						Settings.Default.ResponseDocBodyTemplateOnUnknownProvider),
						currentUID);
			}
			catch (Exception exMatch)
			{
				_logger.Error("�� ������� ��������� �������������� ������", exMatch);
			}
		}

		private static void SendErrorLetterToProvider(AddressList FromList, 
			string causeSubject, string causeBody, Mime sourceLetter)
		{
			try
			{
				var _from = new AddressList();
				_from.Parse("farm@analit.net");

				//Mime responseMime = Mime.CreateSimple(_from, FromList, causeSubject, causeBody, String.Empty);
				var responseMime = new Mime();
				responseMime.MainEntity.From = _from;
#if DEBUG
				var toList = new AddressList { new MailboxAddress(Settings.Default.SMTPUserFail) };
				responseMime.MainEntity.To = toList;
#else
				responseMime.MainEntity.To = FromList;
#endif
				responseMime.MainEntity.Subject = causeSubject;
				responseMime.MainEntity.ContentType = MediaType_enum.Multipart_mixed;

				var testEntity  = responseMime.MainEntity.ChildEntities.Add();
				testEntity.ContentType = MediaType_enum.Text_plain;
				testEntity.ContentTransferEncoding = ContentTransferEncoding_enum.QuotedPrintable;
				testEntity.DataText = causeBody;

				var attachEntity  = responseMime.MainEntity.ChildEntities.Add();
				attachEntity.ContentType = MediaType_enum.Application_octet_stream;
				attachEntity.ContentTransferEncoding = ContentTransferEncoding_enum.Base64;
				attachEntity.ContentDisposition = ContentDisposition_enum.Attachment;
				attachEntity.ContentDisposition_FileName = (!String.IsNullOrEmpty(sourceLetter.MainEntity.Subject)) ? sourceLetter.MainEntity.Subject + ".eml" : "Unrec.eml";
				attachEntity.Data = sourceLetter.ToByteData();

				LumiSoft.Net.SMTP.Client.SmtpClientEx.QuickSendSmartHost(Settings.Default.SMTPHost, 25, String.Empty, responseMime);
			}
			catch
			{ }
		}

		private int? GetFirmCodeByFromList(AddressList FromList)
		{
			try
			{
				foreach (MailboxAddress address in FromList)
				{
					var FirmCode = MethodTemplate.ExecuteMethod(
						new ExecuteArgs(),
						delegate {
							return MySqlHelper.ExecuteScalar(
								_workConnection,
								String.Format(@"
SELECT w.FirmCode 
FROM documents.waybill_sources w 
WHERE w.EMailFrom LIKE '%{0}%' AND w.SourceID = 1", address.EmailAddress)); ;
						},
						null,
						_workConnection,
						true,
						false,
						(e, ex) => Ping());
						
					if (FirmCode != null)
						return Convert.ToInt32(FirmCode);
				}
				return null;
			}
			catch
			{
				return null;
			}
		}

		protected override string GetFailMail()
		{
			return Settings.Default.DocumentFailMail;
		}

		protected override void SendUnrecLetter(Mime m, AddressList FromList, 
			string AttachNames, string cause)
		{
			try
			{
				var ms = new MemoryStream(m.ToByteData());
				FailMailSend(m.MainEntity.Subject, FromList.ToAddressListString(), 
					m.MainEntity.To.ToAddressListString(), m.MainEntity.Date, ms, AttachNames, cause);

				WriteLog((_currentDocumentType != null) ? (int?)_currentDocumentType.TypeID : null,
					GetFirmCodeByFromList(FromList), _aptekaClientCode, null,
					String.Format(@"{0} 
����            : {1} 
�����������     : {2}
����������      : {3}
������ �������� : 
{4}
", 
						cause, 
						m.MainEntity.Subject, 
						FromList.ToAddressListString(), 
						m.MainEntity.To.ToAddressListString(), 
						AttachNames),
						currentUID);
			}
			catch (Exception exMatch)
			{
				_logger.Error("�� ������� ��������� �������������� ������", exMatch);
			}
		}

		protected override bool ProcessAttachs(Mime m, AddressList FromList, 
			ref string causeSubject, ref string causeBody)
		{
			//���� �� ����������� ������ ������ � ����������, ����� - ������ �� ����������
			bool matched = false;

			DataRow[] drLS;

			/*
			 * � ��������� ������ �������������� ������� ��-�������: 
			 * ������ �������������� ������������ ������ �����������
			 * � ���� ����� ����������� ������ � ���������, �� ��� �������� 
			 * ����������� ������������ ����.
			 * ���� �� �� ������, �� ������ �� ������.
			 */
			foreach (var mbFrom in FromList.Mailboxes)
			{
				drLS = dtSources.Select(String.Format("({0} like '*{1}*')",
					WaybillSourcesTable.colEMailFrom, mbFrom.EmailAddress));
				// ����� ����������� ������ ���� ������ � ������ ����������, 
				// ���� ���������� ������, �� ��� ������
				if (drLS.Length == 1)
				{
					var drS = drLS[0];

					foreach (var entity in m.Attachments)
					{
						if (!String.IsNullOrEmpty(entity.ContentDisposition_FileName) || 
							!String.IsNullOrEmpty(entity.ContentType_Name))
						{
							SaveAttachement(entity);
							var CorrectArchive = CheckFile();
							matched = true;
							if (CorrectArchive)
							{
								ProcessWaybillFile(CurrFileName, drS);
							}
							else
							{
								WriteLog(_currentDocumentType.TypeID, 
									Convert.ToInt32(drS[WaybillSourcesTable.colFirmCode]), 
									_aptekaClientCode, Path.GetFileName(CurrFileName), 
									"�� ������� ����������� ����", currentUID);
							}
							DeleteCurrFile();
						}
					}

					drS.Delete();			
				}
				else
					if (drLS.Length > 1)
					{
						throw new Exception(String.Format("�� ����� \"{0}\" ��������� ��������� �����������.", 
							mbFrom.EmailAddress));
					}
				dtSources.AcceptChanges();
			}//foreach (MailboxAddress mbFrom in FromList.Mailboxes)

			if (!matched)
				causeBody = "�� ������ ��������.";
			return matched;
		}

		protected void ProcessWaybillFile(string InFile, DataRow drCurrent)
		{
			//������ ������ 
			var Files = new[] { InFile };
			if (ArchiveHelper.IsArchive(InFile))
			{
				Files = Directory.GetFiles(InFile + ExtrDirSuffix + 
					Path.DirectorySeparatorChar, "*.*", SearchOption.AllDirectories);
			}
			foreach (string s in Files)
			{
				MoveWaybill(s, drCurrent);
			}
		}

		protected void MoveWaybill(string FileName, DataRow drCurrent)
		{
			bool Quit = false;

			//�������� ������������� ��� ����� 
			string _convertedFileName = FileHelper.FileNameToWindows1251(Path.GetFileName(FileName));
			if (!_convertedFileName.Equals(Path.GetFileName(FileName), StringComparison.CurrentCultureIgnoreCase))
			{
				//���� ��������� �������������� ���������� �� ��������� �����, �� ��������������� ����
				_convertedFileName = Path.GetDirectoryName(FileName) + 
					Path.DirectorySeparatorChar + _convertedFileName;

				File.Move(FileName, _convertedFileName);
				FileName = _convertedFileName;
			}

			var addressId = _aptekaClientCode;
			var clientId = GetClientIdByAddress(ref addressId);
			if (clientId == null)
			{
				clientId = _aptekaClientCode;
				addressId = null;
			}

			var cmdInsert = new MySqlCommand(@"
INSERT INTO logs.document_logs (FirmCode, ClientCode, FileName, MessageUID, DocumentType, AddressId)
VALUES (?FirmCode, ?ClientCode, ?FileName, ?MessageUID, ?DocumentType, ?AddressId); select last_insert_id();", _workConnection);

			cmdInsert.Parameters.AddWithValue("?FirmCode", drCurrent[WaybillSourcesTable.colFirmCode]);
			cmdInsert.Parameters.AddWithValue("?ClientCode", clientId);
			cmdInsert.Parameters.AddWithValue("?FileName", Path.GetFileName(FileName));
			cmdInsert.Parameters.AddWithValue("?MessageUID", currentUID);
			cmdInsert.Parameters.AddWithValue("?DocumentType", _currentDocumentType.TypeID);
			if (addressId == null)
				cmdInsert.Parameters.AddWithValue("?AddressId", DBNull.Value);
			else
				cmdInsert.Parameters.AddWithValue("?AddressId", addressId);

			MySqlTransaction transaction = null;

			var AptekaClientDirectory = FileHelper.NormalizeDir(Settings.Default.FTPOptBoxPath) + 
				_aptekaClientCode.ToString().PadLeft(3, '0') + Path.DirectorySeparatorChar + _currentDocumentType.FolderName;
			var OutFileNameTemplate = AptekaClientDirectory + Path.DirectorySeparatorChar;
			var OutFileName = String.Empty;

			do
			{
				try
				{
					if (_workConnection.State != ConnectionState.Open)
						_workConnection.Open();

					if (!Directory.Exists(AptekaClientDirectory))
						Directory.CreateDirectory(AptekaClientDirectory);

					transaction = _workConnection.BeginTransaction(IsolationLevel.RepeatableRead);

					cmdInsert.Transaction = transaction;

					OutFileName = OutFileNameTemplate + cmdInsert.ExecuteScalar() + "_"
						+ drCurrent[WaybillSourcesTable.colShortName]
						+ "(" + Path.GetFileNameWithoutExtension(FileName) + ")"
						+ Path.GetExtension(FileName);

					OutFileName = PriceProcessor.FileHelper.NormalizeFileName(OutFileName);

					if (File.Exists(OutFileName))
						try
						{
							File.Delete(OutFileName);
						}
						catch { }

					File.Move(FileName, OutFileName);

					transaction.Commit();

					Quit = true;
					// ��������� ��������� � ��������� ����������
					SaveWaybill(_aptekaClientCode, _currentDocumentType, OutFileName);
				}
				catch (MySqlException MySQLErr)
				{
					if (transaction != null)
					{
						transaction.Rollback();
						transaction = null;
					}

					if ((MySQLErr.Number == 1205) || (MySQLErr.Number == 1213) || (MySQLErr.Number == 1422))
					{
						_logger.Error("ExecuteCommand.������", MySQLErr);
						Ping();
						System.Threading.Thread.Sleep(5000);
						Ping();
					}
					else
						throw;
				}
				catch 
				{
					if (transaction != null)
					{
						transaction.Rollback();
					}
					if (!String.IsNullOrEmpty(OutFileName) && File.Exists(OutFileName))
						try
						{
							File.Delete(OutFileName);
						}
						catch { }
					throw;
				}
			} while (!Quit);
		}

		private void WriteLog(int? DocumentType, int? FirmCode, int? ClientCode,
			string FileName, string Addition, int MessageUID)
		{
			var addressId = ClientCode;
			int? clientId = GetClientIdByAddress(ref addressId);
			if (clientId == null)
			{
				clientId = ClientCode;
				addressId = null;
			}

			MethodTemplate.ExecuteMethod<ExecuteArgs, object>(new ExecuteArgs(), 
				delegate(ExecuteArgs args) {
					var cmdInsert = new MySqlCommand(@"
INSERT INTO logs.document_logs (FirmCode, ClientCode, FileName, Addition, MessageUID, DocumentType, AddressId) 
VALUES (?FirmCode, ?ClientCode, ?FileName, ?Addition, ?MessageUID, ?DocumentType, ?AddressId)", args.DataAdapter.SelectCommand.Connection);

					cmdInsert.Parameters.AddWithValue("?FirmCode", FirmCode);
					cmdInsert.Parameters.AddWithValue("?ClientCode", clientId);
					cmdInsert.Parameters.AddWithValue("?FileName", FileName);
					cmdInsert.Parameters.AddWithValue("?Addition", Addition);
					cmdInsert.Parameters.AddWithValue("?MessageUID", MessageUID);
					cmdInsert.Parameters.AddWithValue("?DocumentType", DocumentType);
					if (addressId == null)
						cmdInsert.Parameters.AddWithValue("?AddressId", DBNull.Value);
					else
						cmdInsert.Parameters.AddWithValue("?AddressId", addressId);
					cmdInsert.ExecuteNonQuery();
					return null;
				},
				null,
				_workConnection,
				true,
				false,
				(e, ex) => Ping());
		}
	}
}
