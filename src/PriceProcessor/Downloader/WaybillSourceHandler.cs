using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.Mime;
using Inforoom.PriceProcessor.Properties;
using Inforoom.Formalizer;
using LumiSoft.Net.IMAP;
using MySql.Data.MySqlClient;
using System.IO;
using ExecuteTemplate;
using Inforoom.Downloader.Documents;
using Inforoom.Common;


namespace Inforoom.Downloader
{

	public class WaybillSourceHandler : EMAILSourceHandler
	{

		private int? AptekaClientCode = null;

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
			AptekaClientCode = null;
			currentType = null;
			int CorrectAddresCount = CorrectClientAddress(m.MainEntity.To, ref EmailList);
			//Все хорошо, если кол-во вложений больше 0 и распознан только один адрес как корректный
			bool res = (m.Attachments.Length > 0) && (CorrectAddresCount == 1);
			//Если не сопоставили с клиентом
			if (CorrectAddresCount == 0)
			{
				systemError = "Не найден клиент.";

				causeSubject = Settings.Default.ResponseDocSubjectTemplateOnNonExistentClient;
				causeBody = Settings.Default.ResponseDocBodyTemplateOnNonExistentClient;
			}
			else
				//Если нет вложений
				if ((CorrectAddresCount == 1) && (m.Attachments.Length == 0))
				{
					systemError = "Письмо не содержит вложений.";

					causeSubject = Settings.Default.ResponseDocSubjectTemplateOnNothingAttachs;
					causeBody = Settings.Default.ResponseDocBodyTemplateOnNothingAttachs;
				}
				else
					//Если несколько клиентов в списке получателей
					if (CorrectAddresCount > 1)
					{
						systemError = "Письмо отправленно нескольким клиентам.";

						causeSubject = Settings.Default.ResponseDocSubjectTemplateOnMultiDomen;
						causeBody = Settings.Default.ResponseDocBodyTemplateOnMultiDomen;
					}
					else
						if (m.Attachments.Length > 0)
						{ 
							bool attachmentsIsBigger = false;
							foreach(MimeEntity attachment in m.Attachments)
								if ((attachment.Data.Length / 1024.0) > Settings.Default.MaxWaybillAttachmentSize)
								{
									attachmentsIsBigger = true;
									break;
								}
							if (attachmentsIsBigger)
							{
								res = false;

								systemError = String.Format("Письмо содержит вложение размером больше максимально допустимого значения ({0} Кб).", Settings.Default.MaxWaybillAttachmentSize);

								causeSubject = Settings.Default.ResponseDocSubjectTemplateOnMaxWaybillAttachment;
								causeBody = String.Format(Settings.Default.ResponseDocBodyTemplateOnMaxWaybillAttachment, Settings.Default.MaxWaybillAttachmentSize);
							}
						}
			return res;
		}

		private bool ClientExists(int CheckClientCode)
		{
			return ExecuteTemplate.MethodTemplate.ExecuteMethod<ExecuteArgs, bool>(
				new ExecuteArgs(), 
				delegate(ExecuteArgs args)
				{
					object clientCode = MySqlHelper.ExecuteScalar(cWork, "select FirmCode from usersettings.clientsdata where FirmType = 1 and FirmCode = " + CheckClientCode.ToString());

					return (clientCode != null);
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

		private int? GetClientCode(string Address)
		{ 
			Address = Address.ToLower();
			InboundDocumentType testType = null;
			int? testClientCode = null;

			foreach (InboundDocumentType id in types)
			{
				int clientCode = 0;
				if (id.ParseEmail(Address, out clientCode))
				{
					testClientCode = clientCode;
					testType = id;
					break;
				}
			}

			if (testType != null)
			{
				if (ClientExists(testClientCode.Value))
				{
					if (currentType == null)
					{
						currentType = testType;
						AptekaClientCode = testClientCode;
					}
				}
				else
					testClientCode = null;
			}

			return testClientCode;
		}

		private int CorrectClientAddress(AddressList addressList, ref string EmailList)
		{
			int? CurrentClientCode = null;
			int ClientCodeCount = 0;

			//Пробегаемся по всем адресам TO и ищем адрес вида <\d+@waybills.analit.net> или <\d+@refused.analit.net>
			//Если таких адресов несколько, то считаем, что письмо ошибочное и не разбираем его дальше
			foreach(MailboxAddress ma in  addressList.Mailboxes)
			{
				CurrentClientCode = GetClientCode(GetCorrectEmailAddress(ma.EmailAddress));
				if (CurrentClientCode.HasValue)
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
			return @"
SELECT
  cd.FirmCode,
  cd.ShortName,
  r.Region as RegionName,
  st.EMailFrom
FROM
usersettings.ClientsData AS Apteka,
Documents.Waybill_Sources             as st
INNER JOIN usersettings.ClientsData AS CD ON CD.FirmCode = st.FirmCode
inner join farm.regions             as r  on r.RegionCode = cd.RegionCode
WHERE
cd.FirmStatus   = 1
and Apteka.FirmCode = ?AptekaClientCode
and st.SourceID = 1";
		}

		protected override DataTable GetSourcesTable(ExecuteArgs e)
		{
			dtSources.Clear();
			daFillSources.SelectCommand.Transaction = e.DataAdapter.SelectCommand.Transaction;
			daFillSources.SelectCommand.Parameters.Clear();
			daFillSources.SelectCommand.Parameters.AddWithValue("?AptekaClientCode", AptekaClientCode);
			daFillSources.Fill(dtSources);
			return dtSources;
		}

		protected override void ErrorOnCheckMime(Mime m, AddressList FromList, string AttachNames, string causeSubject, string causeBody, string systemError)
		{
			if (causeBody != String.Empty)
			{
				SendErrorLetterToProvider(FromList, causeSubject, causeBody, m);
				WriteLog(
					(currentType != null) ? (int?)currentType.TypeID : null,
					GetFirmCodeByFromList(FromList), 
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

		protected override void ErrorOnProcessAttachs(Mime m, AddressList FromList, string AttachNames, string causeSubject, string causeBody)
		{
			try
			{
				string cause = "Для данного E-mail не найден источник в таблице documents.waybill_sources";
				MemoryStream ms = new MemoryStream(m.ToByteData());
				SendErrorLetterToProvider(
					FromList, 
					Settings.Default.ResponseDocSubjectTemplateOnUnknownProvider, 
					Settings.Default.ResponseDocBodyTemplateOnUnknownProvider, 
					m);
				FailMailSend(m.MainEntity.Subject, FromList.ToAddressListString(), m.MainEntity.To.ToAddressListString(), m.MainEntity.Date, ms, AttachNames, cause);
				WriteLog(
					(currentType != null) ? (int?)currentType.TypeID : null,
					GetFirmCodeByFromList(FromList),
					AptekaClientCode,
					null,
					String.Format(@"{0} 
Отправители     : {1}
Получатели      : {2}
Список вложений : 
{3}
Тема письма поставщику : {4}
Тело письма поставщику : 
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
				_logger.Error("Не удалось отправить нераспознанное письмо", exMatch);
			}
		}

		private static void SendErrorLetterToProvider(AddressList FromList, string causeSubject, string causeBody, Mime sourceLetter)
		{
			try
			{
				AddressList _from = new AddressList();
				_from.Parse("farm@analit.net");

				//Mime responseMime = Mime.CreateSimple(_from, FromList, causeSubject, causeBody, String.Empty);
				Mime responseMime = new Mime();
				responseMime.MainEntity.From = _from;
				responseMime.MainEntity.To = FromList;
				responseMime.MainEntity.Subject = causeSubject;
				responseMime.MainEntity.ContentType = MediaType_enum.Multipart_mixed;

				MimeEntity testEntity  = responseMime.MainEntity.ChildEntities.Add();
				testEntity.ContentType = MediaType_enum.Text_plain;
				testEntity.ContentTransferEncoding = ContentTransferEncoding_enum.QuotedPrintable;
				testEntity.DataText = causeBody;

				MimeEntity attachEntity  = responseMime.MainEntity.ChildEntities.Add();
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
					object FirmCode = ExecuteTemplate.MethodTemplate.ExecuteMethod<ExecuteArgs, object>(
						new ExecuteArgs(),
						delegate(ExecuteArgs args)
						{
							return MySqlHelper.ExecuteScalar(
								cWork,
								String.Format("select w.FirmCode FROM documents.waybill_sources w WHERE w.EMailFrom like '%{0}%' and w.SourceID = 1", address.EmailAddress)); ;
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

		protected override void SendUnrecLetter(Mime m, AddressList FromList, string AttachNames, string cause)
		{
			try
			{
				MemoryStream ms = new MemoryStream(m.ToByteData());
				FailMailSend(m.MainEntity.Subject, FromList.ToAddressListString(), m.MainEntity.To.ToAddressListString(), m.MainEntity.Date, ms, AttachNames, cause);
				WriteLog(
					(currentType != null) ? (int?)currentType.TypeID : null,
					GetFirmCodeByFromList(FromList),
					AptekaClientCode,
					null,
					String.Format(@"{0} 
Тема            : {1} 
Отправители     : {2}
Получатели      : {3}
Список вложений : 
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
				_logger.Error("Не удалось отправить нераспознанное письмо", exMatch);
			}
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
					WaybillSourcesTable.colEMailFrom, mbFrom.EmailAddress));
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
								WriteLog(currentType.TypeID, Convert.ToInt32(drS[WaybillSourcesTable.colFirmCode]), AptekaClientCode, Path.GetFileName(CurrFileName), "Не удалось распаковать файл", currentUID);
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
			if (ArchiveHelper.IsArchive(InFile))
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

			//Пытаемся преобразовать имя файла 
			string _convertedFileName = FileHelper.FileNameToWindows1251(Path.GetFileName(FileName));
			if (!_convertedFileName.Equals(Path.GetFileName(FileName), StringComparison.CurrentCultureIgnoreCase))
			{
				//Если результат преобразования отличается от исходного имени, то переименовываем файл
				_convertedFileName = Path.GetDirectoryName(FileName) + Path.DirectorySeparatorChar + _convertedFileName;
				File.Move(FileName, _convertedFileName);
				FileName = _convertedFileName;
			}

			MySqlCommand cmdInsert = new MySqlCommand("insert into logs.document_logs (FirmCode, ClientCode, FileName, MessageUID, DocumentType) values (?FirmCode, ?ClientCode, ?FileName, ?MessageUID, ?DocumentType); select last_insert_id();", cWork);
			cmdInsert.Parameters.AddWithValue("?FirmCode", drCurrent[WaybillSourcesTable.colFirmCode]);
			cmdInsert.Parameters.AddWithValue("?ClientCode", AptekaClientCode);
			cmdInsert.Parameters.AddWithValue("?FileName", Path.GetFileName(FileName));
			cmdInsert.Parameters.AddWithValue("?MessageUID", currentUID);
			cmdInsert.Parameters.AddWithValue("?DocumentType", currentType.TypeID);			

			MySqlTransaction tran = null;

			string AptekaClientDirectory = FileHelper.NormalizeDir(Settings.Default.FTPOptBoxPath) + AptekaClientCode.ToString().PadLeft(3, '0') + Path.DirectorySeparatorChar + currentType.FolderName;
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

					tran = cWork.BeginTransaction(IsolationLevel.RepeatableRead);

					cmdInsert.Transaction = tran;

					OutFileName = OutFileNameTemplate + cmdInsert.ExecuteScalar().ToString() + "_"
						+ drCurrent[WaybillSourcesTable.colShortName].ToString()
						+ "(" + Path.GetFileNameWithoutExtension(FileName) + ")"
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
					if (tran != null)
					{
						tran.Rollback();
						tran = null;
					}

					if ((MySQLErr.Number == 1205) || (MySQLErr.Number == 1213) || (MySQLErr.Number == 1422))
					{
						_logger.Error("ExecuteCommand.Повтор", MySQLErr);
						Ping();
						System.Threading.Thread.Sleep(5000);
						Ping();
					}
					else
						throw;
				}
				catch 
				{
					if (tran != null)
					{
						tran.Rollback();
						tran = null;
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

		private void WriteLog(int? DocumentType, int? FirmCode, int? ClientCode, string FileName, string Addition, int MessageUID)
		{
			MethodTemplate.ExecuteMethod<ExecuteArgs, object>(new ExecuteArgs(), delegate(ExecuteArgs args)
			{
				MySqlCommand cmdInsert = new MySqlCommand("insert into logs.document_logs (FirmCode, ClientCode, FileName, Addition, MessageUID, DocumentType) values (?FirmCode, ?ClientCode, ?FileName, ?Addition, ?MessageUID, ?DocumentType)", args.DataAdapter.SelectCommand.Connection);

				cmdInsert.Parameters.AddWithValue("?FirmCode", FirmCode);
				cmdInsert.Parameters.AddWithValue("?ClientCode", ClientCode);
				cmdInsert.Parameters.AddWithValue("?FileName", FileName);
				cmdInsert.Parameters.AddWithValue("?Addition", Addition);
				cmdInsert.Parameters.AddWithValue("?MessageUID", MessageUID);
				cmdInsert.Parameters.AddWithValue("?DocumentType", DocumentType);
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


	}
}
