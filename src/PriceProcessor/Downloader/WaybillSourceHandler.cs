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

		protected override bool CheckMime(Mime m)
		{
			return (m.Attachments.Length > 0) && (CorrectClientAddress(m.MainEntity.To));
		}

		private int GetClientCode(string Address)
		{ 
			Address = Address.ToLower();
			int Index = Address.IndexOf("@waybills.analit.net");
			if (Index > -1)
			{
				AptekaClientCode = 0;
				if (int.TryParse(Address.Substring(0, Index), out AptekaClientCode))
					return AptekaClientCode;
				else
					return -1;				
			}
			else
				return -1;
		}

		private bool CorrectClientAddress(AddressList addressList)
		{
			bool Find = false;
			int CurrentClientCode = -1;

			//Пробегаемся по всем адресам TO и ищем адрес вида <\d+@waybills.analit.net>
			//Если таких адресов несколько, то считаем, что письмо ошибочное и не разбираем его дальше
			foreach(MailboxAddress ma in  addressList.Mailboxes)
			{
				CurrentClientCode = GetClientCode(ma.EmailAddress);
				if (CurrentClientCode > -1)
				{
					if (!Find)
						Find = true;
					else
						return false;				
				}
			}
			return Find;
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
and Apteka.FirmCode = ?AptekaClientCode",
				Settings.Default.tbClientsData,
				Settings.Default.tbWaybillSources);
		}

		protected override void FillSourcesTable()
		{
			dtSources.Clear();
			try
			{
				daFillSources.SelectCommand.Parameters.Clear();
				daFillSources.SelectCommand.Parameters.Add("AptekaClientCode", AptekaClientCode);
				daFillSources.Fill(dtSources);
			}
			catch (Exception ex)
			{
				FormLog.Log(this.GetType().Name + ".FillSourcesTable", ex.ToString());
			}
		}

		private override void ProcessAttachs(Mime m, ref bool Matched, AddressList FromList, ref string AttachNames)
		{
			bool CorrectArchive = true;
			string ShortFileName = string.Empty;

			DataRow[] drLS = null;

			//Раньше не проверялся весь список From, теперь это делается. Туда же добавляется и Sender
			foreach (MailboxAddress mbFrom in FromList.Mailboxes)
			{
				drLS = dtSources.Select(String.Format("({0} like '*{1}*')",
					WaybillSourcesTable.colEMailFrom, mbFrom.EmailAddress));
				if (drLS.Length == 1)
				{
					DataRow drS = drLS[0];

					foreach (MimeEntity ent in m.Attachments)
					{
						if (!String.IsNullOrEmpty(ent.ContentDisposition_FileName) || !String.IsNullOrEmpty(ent.ContentType_Name))
						{
							ShortFileName = SaveAttachement(ent);
							AttachNames += "\"" + ShortFileName + "\"" + Environment.NewLine;
							CorrectArchive = CheckFile();
							Matched = true;
							if (CorrectArchive)
							{
								ProcessWaybillFile(CurrFileName, drS);
							}
							else
							{
								//TODO: надо что-то делать с такими файлами
								//Logging(CurrPriceCode, "Не удалось распаковать файл '" + Path.GetFileName(CurrFileName) + "'");
							}
							DeleteCurrFile();
						}
					}

					Matched = true;
					if (CorrectArchive)
					{
						ProcessWaybillFile(CurrFileName, drS);
					}
					else
					{
						//TODO: надо что-то делать с такими файлами
						//Logging(CurrPriceCode, "Не удалось распаковать файл '" + Path.GetFileName(CurrFileName) + "'");
					}
					drS.Delete();

			
					foreach (DataRow drS in drLS)
					{
						Matched = true;
						//drCurrent = drS;
						//SetCurrentPriceCode(drS);
						if (CorrectArchive)
						{
							ProcessWaybillFile(CurrFileName, drS);
							//if (ProcessPriceFile(CurrFileName))
							//{
							//    Logging(CurrPriceCode, String.Empty);
							//    UpdateDB(CurrPriceCode, DateTime.Now);
							//}
							//else
							//{
							//    Logging(CurrPriceCode, "Не удалось обработать файл '" + Path.GetFileName(CurrFileName) + "'");
							//}
						}
						else
						{
							//TODO: надо что-то делать с такими файлами
							//Logging(CurrPriceCode, "Не удалось распаковать файл '" + Path.GetFileName(CurrFileName) + "'");
						}
						drS.Delete();
					}
				}
				else
					if (drLS.Length > 1)
						throw new Exception(String.Format("На адрес \"{0}\" назначено несколько поставщиков.", mbFrom.EmailAddress));
				dtSources.AcceptChanges();
			}//foreach (MailboxAddress mbFrom in FromList.Mailboxes)



			foreach (MimeEntity ent in m.Attachments)
			{
				if (!String.IsNullOrEmpty(ent.ContentDisposition_FileName) || !String.IsNullOrEmpty(ent.ContentType_Name))
				{
					ShortFileName = SaveAttachement(ent);
					AttachNames += "\"" + ShortFileName + "\"" + Environment.NewLine;
					CorrectArchive = CheckFile();
					UnPack(m, ref Matched, FromList, ShortFileName, CorrectArchive);
					DeleteCurrFile();
				}
			}
		}

		protected override void UnPack(Mime m, ref bool Matched, AddressList FromList, string ShortFileName, bool CorrectArchive)
		{
			DataRow[] drLS = null;

			//Раньше не проверялся весь список From, теперь это делается. Туда же добавляется и Sender
			foreach (MailboxAddress mbFrom in FromList.Mailboxes)
			{
				drLS = dtSources.Select(String.Format("({0} like '*{1}*')",
					WaybillSourcesTable.colEMailFrom, mbFrom.EmailAddress));
				if (drLS.Length == 1)
					foreach (DataRow drS in drLS)
					{
						Matched = true;
						//drCurrent = drS;
						//SetCurrentPriceCode(drS);
						if (CorrectArchive)
						{
							ProcessWaybillFile(CurrFileName, drS);
							//if (ProcessPriceFile(CurrFileName))
							//{
							//    Logging(CurrPriceCode, String.Empty);
							//    UpdateDB(CurrPriceCode, DateTime.Now);
							//}
							//else
							//{
							//    Logging(CurrPriceCode, "Не удалось обработать файл '" + Path.GetFileName(CurrFileName) + "'");
							//}
						}
						else
						{
							//TODO: надо что-то делать с такими файлами
							//Logging(CurrPriceCode, "Не удалось распаковать файл '" + Path.GetFileName(CurrFileName) + "'");
						}
						drS.Delete();
					}
				else
					throw new Exception(String.Format("На адрес \"{0}\" назначено несколько поставщиков.", mbFrom.EmailAddress));
				dtSources.AcceptChanges();
			}//foreach (MailboxAddress mbFrom in FromList.Mailboxes)
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
			MySqlCommand cmdInsert = new MySqlCommand("insert into logs.waybill_receive_logs (FirmCode, ClientCode, FileName) values (?FirmCode, ?ClientCode, ?FileName); select last_insert_id();", cWork);
			cmdInsert.Parameters.Add("FirmCode", drCurrent[WaybillSourcesTable.colFirmCode]);
			cmdInsert.Parameters.Add("ClientCode", AptekaClientCode);
			cmdInsert.Parameters.Add("FileName", Path.GetFileName(FileName));

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

	}
}
