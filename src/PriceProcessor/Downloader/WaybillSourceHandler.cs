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


namespace Inforoom.Downloader
{

	//Класс содержит название полей из таблицы Sources
	public sealed class WaybillSourcesTable
	{
		public static string colFirmCode = "FirmCode";
		public static string colShortName = "ShortName";
		public static string colEMailFrom = "EMailFrom";
	}

	public class WaybillSourceHandler : EMAILSourceHandler
	{

		private int AptekaClientCode = 0;

		public WaybillSourceHandler(string sourceType)
            : base(sourceType)
        { }

		protected override void IMAPAuth(IMAP_Client c)
		{
			c.Authenticate(Settings.Default.WaybillIMAPUser, Settings.Default.WaybillIMAPPass);
		}

		protected override bool CheckMime(Mime m, ref string causeSubject, ref string causeBody, ref string systemError)
		{
			string EmailList = String.Empty;
			int CorrectAddresCount = CorrectClientAddress(m.MainEntity.To, ref EmailList);
			bool res = (m.Attachments.Length > 0) && (CorrectAddresCount == 1);
			//Если не сопоставили с клиентом
			if (CorrectAddresCount == 0)
			{
				systemError = "Не найден клиент.";

				causeSubject = Settings.Default.ResponseWaybillSubjectTemplateOnNonExistentClient;
				causeBody = Settings.Default.ResponseWaybillBodyTemplateOnNonExistentClient;
			}
			else
				//Если нет вложений
				if ((CorrectAddresCount == 1) && (m.Attachments.Length == 0))
				{
					systemError = "Письмо не содержит вложений.";
					string Address = AptekaClientCode.ToString() + "@waybills.analit.net";
					string LetterDate = m.MainEntity.Date.ToString("yyyy.MM.dd HH.mm.ss");

					causeSubject = String.Format(Settings.Default.ResponseWaybillSubjectTemplateOnNothingAttachs, Address, LetterDate);
					causeBody = String.Format(Settings.Default.ResponseWaybillBodyTemplateOnNothingAttachs, Address, LetterDate);
				}
				else
					//Если несколько клиентов в списке получателей
					if (CorrectAddresCount > 1)
					{
						systemError = "Письмо отправленно нескольким клиентам.";
						string LetterDate = m.MainEntity.Date.ToString("yyyy.MM.dd HH.mm.ss");

						causeSubject = Settings.Default.ResponseWaybillSubjectTemplateOnMultiDomen;
						causeBody = String.Format(Settings.Default.ResponseWaybillBodyTemplateOnMultiDomen, LetterDate, EmailList);
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
			int Index = Address.IndexOf("@waybills.analit.net");
			if (Index > -1)
			{
				AptekaClientCode = 0;
				if (int.TryParse(Address.Substring(0, Index), out AptekaClientCode))
				{
					if (!ClientExists(AptekaClientCode))
						AptekaClientCode = 0;
					return AptekaClientCode;
				}
				else
					return 0;				
			}
			else
				return 0;
		}

		private int CorrectClientAddress(AddressList addressList, ref string EmailList)
		{
			int CurrentClientCode = 0;
			int ClientCodeCount = 0;

			//Пробегаемся по всем адресам TO и ищем адрес вида <\d+@waybills.analit.net>
			//Если таких адресов несколько, то считаем, что письмо ошибочное и не разбираем его дальше
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
					0, 
					AptekaClientCode, 
					null, 
					String.Format(@"{0}
Отправители            : {1}
Получатели             : {2}
Список вложений        : 
{3}
Тема письма поставщику : {4}
Тело письма поставщику : 
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
				SendUnrecLetter(m, FromList, AttachNames, "Не распознанное письмо.");
		}

		protected override string GetFailMail()
		{
			return "tech@analit.net";
		}

		protected override bool ProcessAttachs(Mime m, AddressList FromList, ref string causeSubject, ref string causeBody)
		{
			//Один из аттачментов письма совпал с источником, иначе - письмо не распознано
			bool _Matched = false;

			bool CorrectArchive = true;
			string ShortFileName = string.Empty;

			DataRow[] drLS = null;

			/*В накладных письма обрабатываются немного по-другому: письма обрабатываются относительно адреса отправителя
			 * и если такой отправитель найден в истониках, то все вложения сохраняются относительно него.
			 * Если он не найден, то ничего не делаем.
			 */
			foreach (MailboxAddress mbFrom in FromList.Mailboxes)
			{
				drLS = dtSources.Select(String.Format("({0} like '*{1}*')",
					WaybillSourcesTable.colEMailFrom, GetCorrectEmailAddress(mbFrom.EmailAddress)));
				//Адрес отправителя должен быть только у одного поставщика, если получилось больше, то это ошибка
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
								WriteLog(Convert.ToInt32(drS[WaybillSourcesTable.colFirmCode]), AptekaClientCode, Path.GetFileName(CurrFileName), "Не удалось распаковать файл", currentUID);
							}
							DeleteCurrFile();
						}
					}

					drS.Delete();			
				}
				else
					if (drLS.Length > 1)
						throw new Exception(String.Format("На адрес \"{0}\" назначено несколько поставщиков.", mbFrom.EmailAddress));
				dtSources.AcceptChanges();
			}//foreach (MailboxAddress mbFrom in FromList.Mailboxes)

			if (!_Matched)
				causeBody = "Не найден источник.";
			return _Matched;
		}

		protected void ProcessWaybillFile(string InFile, DataRow drCurrent)
		{
			//Массив файлов 
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
			MySqlCommand cmdInsert = new MySqlCommand("insert into logs.waybill_receive_logs (FirmCode, ClientCode, FileName, MessageUID) values (?FirmCode, ?ClientCode, ?FileName, ?MessageUID); select last_insert_id();", cWork);
			cmdInsert.Parameters.Add("?FirmCode", drCurrent[WaybillSourcesTable.colFirmCode]);
			cmdInsert.Parameters.Add("?ClientCode", AptekaClientCode);
			cmdInsert.Parameters.Add("?FileName", Path.GetFileName(FileName));
			cmdInsert.Parameters.Add("?MessageUID", currentUID);			

			MySqlTransaction tran;

			string AptekaClientDirectory = NormalizeDir(Settings.Default.FTPOptBox) + Path.DirectorySeparatorChar + AptekaClientCode.ToString().PadLeft(3, '0') + Path.DirectorySeparatorChar + "Waybills";
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
						FormLog.Log(this.GetType().Name + ".ExecuteCommand", "Повтор : {0}", MySQLErr);
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

		private void WriteLog(int FirmCode, int ClientCode, string FileName, string Addition, int MessageUID)
		{
			ExecuteTemplate.MethodTemplate.ExecuteMethod<ExecuteArgs, object>(new ExecuteArgs(), delegate(ExecuteArgs args)
			{
				MySqlCommand cmdInsert = new MySqlCommand("insert into logs.waybill_receive_logs (FirmCode, ClientCode, FileName, Addition, MessageUID) values (?FirmCode, ?ClientCode, ?FileName, ?Addition, ?MessageUID)", args.DataAdapter.SelectCommand.Connection);

				cmdInsert.Parameters.Add("?FirmCode", FirmCode);
				cmdInsert.Parameters.Add("?ClientCode", ClientCode);
				cmdInsert.Parameters.Add("?FileName", FileName);
				cmdInsert.Parameters.Add("?Addition", Addition);
				cmdInsert.Parameters.Add("?MessageUID", MessageUID);
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
