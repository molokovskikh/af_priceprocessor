using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.Mime;
using Inforoom.Downloader.Properties;
using Inforoom.Formalizer;
using LumiSoft.Net.IMAP;
using MySql.Data.MySqlClient;
using System.IO;
using ExecuteTemplate;
using Inforoom.Downloader.Documents;


namespace Inforoom.Downloader
{

	//����� �������� �������� ����� �� ������� Sources
	public sealed class WaybillSourcesTable
	{
		public static string colFirmCode = "FirmCode";
		public static string colShortName = "ShortName";
		public static string colEMailFrom = "EMailFrom";
	}

	public class WaybillSourceHandler : EMAILSourceHandler
	{

		private int AptekaClientCode = 0;

		private List<InboundDocumentType> types;

		private InboundDocumentType currentType = null;

		public WaybillSourceHandler()
            : base()
        {
			this.sourceType = "WAYBILL";
			types = new List<InboundDocumentType>();
			types.Add(new WaybillType());
			types.Add(new RejectType());
		}

		protected override void IMAPAuth(IMAP_Client c)
		{
			c.Authenticate(Settings.Default.WaybillIMAPUser, Settings.Default.WaybillIMAPPass);
		}

		protected override bool CheckMime(Mime m, ref string causeSubject, ref string causeBody, ref string systemError)
		{
			string EmailList = String.Empty;
			AptekaClientCode = 0;
			currentType = null;
			int CorrectAddresCount = CorrectClientAddress(m.MainEntity.To, ref EmailList);
			bool res = (m.Attachments.Length > 0) && (CorrectAddresCount == 1);
			//���� �� ����������� � ��������
			if (CorrectAddresCount == 0)
			{
				systemError = "�� ������ ������.";

				causeSubject = Settings.Default.ResponseDocSubjectTemplateOnNonExistentClient;
				causeBody = Settings.Default.ResponseDocBodyTemplateOnNonExistentClient;
			}
			else
				//���� ��� ��������
				if ((CorrectAddresCount == 1) && (m.Attachments.Length == 0))
				{
					systemError = "������ �� �������� ��������.";
					string Address = AptekaClientCode.ToString() + "@" + currentType.Domen;
					string LetterDate = m.MainEntity.Date.ToString("yyyy.MM.dd HH.mm.ss");

					causeSubject = String.Format(Settings.Default.ResponseDocSubjectTemplateOnNothingAttachs, Address, LetterDate);
					causeBody = String.Format(Settings.Default.ResponseDocBodyTemplateOnNothingAttachs, Address, LetterDate);
				}
				else
					//���� ��������� �������� � ������ �����������
					if (CorrectAddresCount > 1)
					{
						systemError = "������ ����������� ���������� ��������.";
						string LetterDate = m.MainEntity.Date.ToString("yyyy.MM.dd HH.mm.ss");

						causeSubject = Settings.Default.ResponseDocSubjectTemplateOnMultiDomen;
						causeBody = String.Format(Settings.Default.ResponseDocBodyTemplateOnMultiDomen, LetterDate, EmailList);
					}
			return res;
		}

		private bool ClientExists(int CheckClientCode)
		{
			return ExecuteTemplate.MethodTemplate.ExecuteMethod<ExecuteArgs, bool>(
				new ExecuteArgs(), 
				delegate(ExecuteArgs args)
				{
					object clientCode = MySqlHelper.ExecuteScalar(cWork, "select ClientCode from usersettings.retclientsset where ClientCode = " + CheckClientCode.ToString());

					return (clientCode != null);
				},
				false,
				cWork,
				true,
				false);
		}

		private int GetClientCode(string Address)
		{ 
			Address = Address.ToLower();
			InboundDocumentType testType = null;
			int testClientCode = 0;

			foreach (InboundDocumentType id in types)
			{
				if (id.ParseEmail(Address, out testClientCode))
				{
					testType = id;
					break;
				}
			}

			if (testType != null)
			{
				if (ClientExists(testClientCode))
				{
					if (currentType == null)
					{
						currentType = testType;
						AptekaClientCode = testClientCode;
					}
				}
				else
					testClientCode = 0;
			}

			return testClientCode;
		}

		private int CorrectClientAddress(AddressList addressList, ref string EmailList)
		{
			int CurrentClientCode = 0;
			int ClientCodeCount = 0;

			//����������� �� ���� ������� TO � ���� ����� ���� <\d+@waybills.analit.net> ��� <\d+@refused.analit.net>
			//���� ����� ������� ���������, �� �������, ��� ������ ��������� � �� ��������� ��� ������
			foreach(MailboxAddress ma in  addressList.Mailboxes)
			{
				CurrentClientCode = GetClientCode(GetCorrectEmailAddress(ma.EmailAddress));
				if (CurrentClientCode > 0)
				{
					if (!String.IsNullOrEmpty(EmailList))
						EmailList += Environment.NewLine;
					EmailList += GetCorrectEmailAddress(ma.EmailAddress);
					ClientCodeCount++;
				}
			}
			return ClientCodeCount;
		}

		protected override string GetSQLSources()
		{
			return String.Format(@"
SELECT
  cd.FirmCode,
  cd.ShortName,
  r.Region as RegionName,
  st.EMailFrom
FROM
           {0} AS Apteka,
           {1}             as st
INNER JOIN {0} AS CD ON CD.FirmCode = st.FirmCode
inner join farm.regions             as r  on r.RegionCode = cd.RegionCode
WHERE
cd.FirmStatus   = 1
and Apteka.FirmCode = ?AptekaClientCode
and st.SourceID = 1",
				Settings.Default.tbClientsData,
				Settings.Default.tbWaybillSources);
		}

		protected override DataTable GetSourcesTable(ExecuteArgs e)
		{
			dtSources.Clear();
			daFillSources.SelectCommand.Transaction = e.DataAdapter.SelectCommand.Transaction;
			daFillSources.SelectCommand.Parameters.Clear();
			daFillSources.SelectCommand.Parameters.Add("AptekaClientCode", AptekaClientCode);
			daFillSources.Fill(dtSources);
			return dtSources;
		}

		protected override void ErrorOnCheckMime(Mime m, AddressList FromList, string AttachNames, string causeSubject, string causeBody, string systemError)
		{
			if (causeBody != String.Empty)
			{
				try
				{
					AddressList _from = new AddressList();
					_from.Parse("farm@analit.net");

					Mime responseMime = Mime.CreateSimple(_from, FromList, causeSubject, causeBody, String.Empty);
					LumiSoft.Net.SMTP.Client.SmtpClientEx.QuickSendSmartHost("box.analit.net", 25, String.Empty, responseMime);
				}
				catch
				{ }
				WriteLog(
					(currentType != null)? currentType.TypeID : 0,
					0, 
					AptekaClientCode, 
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

		protected override string GetFailMail()
		{
			return Settings.Default.DocumentFailMail;
		}

		protected override void SendUnrecLetter(Mime m, AddressList FromList, string AttachNames, string cause)
		{
			try
			{
				MemoryStream ms = new MemoryStream(m.ToByteData());
				FailMailSend(m.MainEntity.Subject, FromList.ToAddressListString(), m.MainEntity.To.ToAddressListString(), m.MainEntity.Date, ms, AttachNames, cause);
				WriteLog(
					(currentType != null) ? currentType.TypeID : 0,
					0,
					AptekaClientCode,
					null,
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
				FormLog.Log(this.GetType().Name, "�� ������� ��������� �������������� ������ : " + exMatch.ToString());
			}
		}


		protected override bool ProcessAttachs(Mime m, AddressList FromList, ref string causeSubject, ref string causeBody)
		{
			//���� �� ����������� ������ ������ � ����������, ����� - ������ �� ����������
			bool _Matched = false;

			bool CorrectArchive = true;
			string ShortFileName = string.Empty;

			DataRow[] drLS = null;

			/*� ��������� ������ �������������� ������� ��-�������: ������ �������������� ������������ ������ �����������
			 * � ���� ����� ����������� ������ � ���������, �� ��� �������� ����������� ������������ ����.
			 * ���� �� �� ������, �� ������ �� ������.
			 */
			foreach (MailboxAddress mbFrom in FromList.Mailboxes)
			{
				drLS = dtSources.Select(String.Format("({0} like '*{1}*')",
					WaybillSourcesTable.colEMailFrom, GetCorrectEmailAddress(mbFrom.EmailAddress)));
				//����� ����������� ������ ���� ������ � ������ ����������, ���� ���������� ������, �� ��� ������
				if (drLS.Length == 1)
				{
					DataRow drS = drLS[0];

					foreach (MimeEntity ent in m.Attachments)
					{
						if (!String.IsNullOrEmpty(ent.ContentDisposition_FileName) || !String.IsNullOrEmpty(ent.ContentType_Name))
						{
							ShortFileName = SaveAttachement(ent);
							CorrectArchive = CheckFile();
							_Matched = true;
							if (CorrectArchive)
							{
								ProcessWaybillFile(CurrFileName, drS);
							}
							else
							{
								WriteLog(currentType.TypeID, Convert.ToInt32(drS[WaybillSourcesTable.colFirmCode]), AptekaClientCode, Path.GetFileName(CurrFileName), "�� ������� ����������� ����", currentUID);
							}
							DeleteCurrFile();
						}
					}

					drS.Delete();			
				}
				else
					if (drLS.Length > 1)
						throw new Exception(String.Format("�� ����� \"{0}\" ��������� ��������� �����������.", mbFrom.EmailAddress));
				dtSources.AcceptChanges();
			}//foreach (MailboxAddress mbFrom in FromList.Mailboxes)

			if (!_Matched)
				causeBody = "�� ������ ��������.";
			return _Matched;
		}

		protected void ProcessWaybillFile(string InFile, DataRow drCurrent)
		{
			//������ ������ 
			string[] Files = new string[] { InFile };
			if (ArchiveHlp.IsArchive(InFile))
			{
				Files = Directory.GetFiles(InFile + ExtrDirSuffix + Path.DirectorySeparatorChar, "*.*", SearchOption.AllDirectories);
			}
			foreach (string s in Files)
			{
				MoveWaybill(s, drCurrent);
			}
		}

		protected void MoveWaybill(string FileName, DataRow drCurrent)
		{
			bool Quit = false;
			MySqlCommand cmdInsert = new MySqlCommand("insert into logs.document_receive_logs (FirmCode, ClientCode, FileName, MessageUID, DocumentType) values (?FirmCode, ?ClientCode, ?FileName, ?MessageUID, ?DocumentType); select last_insert_id();", cWork);
			cmdInsert.Parameters.Add("?FirmCode", drCurrent[WaybillSourcesTable.colFirmCode]);
			cmdInsert.Parameters.Add("?ClientCode", AptekaClientCode);
			cmdInsert.Parameters.Add("?FileName", Path.GetFileName(FileName));
			cmdInsert.Parameters.Add("?MessageUID", currentUID);
			cmdInsert.Parameters.Add("?DocumentType", currentType.TypeID);			

			MySqlTransaction tran;

			string AptekaClientDirectory = NormalizeDir(Settings.Default.FTPOptBox) + Path.DirectorySeparatorChar + AptekaClientCode.ToString().PadLeft(3, '0') + Path.DirectorySeparatorChar + currentType.FolderName;
			string OutFileNameTemplate = AptekaClientDirectory + Path.DirectorySeparatorChar;
			string OutFileName = String.Empty;


			do
			{
				try
				{
					if (cWork.State != ConnectionState.Open)
					{
						cWork.Open();
					}

					if (!Directory.Exists(AptekaClientDirectory))
						Directory.CreateDirectory(AptekaClientDirectory);

					tran = cWork.BeginTransaction();

					cmdInsert.Transaction = tran;

					OutFileName = OutFileNameTemplate + cmdInsert.ExecuteScalar().ToString() + "_"
						+ drCurrent[WaybillSourcesTable.colShortName].ToString()
						+ Path.GetExtension(FileName);

					OutFileName = NormalizeFileName(OutFileName);

					if (File.Exists(OutFileName))
						try
						{
							File.Delete(OutFileName);
						}
						catch { }

					File.Move(FileName, OutFileName);

					tran.Commit();

					Quit = true;
				}
				catch (MySqlException MySQLErr)
				{
					if (MySQLErr.Number == 1213 || MySQLErr.Number == 1205)
					{
						FormLog.Log(this.GetType().Name + ".ExecuteCommand", "������ : {0}", MySQLErr);
						Ping();
						System.Threading.Thread.Sleep(5000);
						Ping();
					}
					else
						throw;
				}
				catch 
				{ 
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

		private void WriteLog(int DocumentType, int FirmCode, int ClientCode, string FileName, string Addition, int MessageUID)
		{
			ExecuteTemplate.MethodTemplate.ExecuteMethod<ExecuteArgs, object>(new ExecuteArgs(), delegate(ExecuteArgs args)
			{
				MySqlCommand cmdInsert = new MySqlCommand("insert into logs.document_receive_logs (FirmCode, ClientCode, FileName, Addition, MessageUID, DocumentType) values (?FirmCode, ?ClientCode, ?FileName, ?Addition, ?MessageUID, ?DocumentType)", args.DataAdapter.SelectCommand.Connection);

				cmdInsert.Parameters.Add("?FirmCode", FirmCode);
				cmdInsert.Parameters.Add("?ClientCode", ClientCode);
				cmdInsert.Parameters.Add("?FileName", FileName);
				cmdInsert.Parameters.Add("?Addition", Addition);
				cmdInsert.Parameters.Add("?MessageUID", MessageUID);
				cmdInsert.Parameters.Add("?DocumentType", DocumentType);
				cmdInsert.ExecuteNonQuery();

				return null;
			},
				null,
				cWork,
				true,
				false);
		}


	}
}
