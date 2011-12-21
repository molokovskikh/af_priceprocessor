﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Common.MySql;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.Mime;
using MySql.Data.MySqlClient;
using System.IO;
using Inforoom.Downloader.Documents;
using Inforoom.Common;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace Inforoom.Downloader
{
	public class DocumentForParsing
	{
		public DocumentForParsing()
		{
			FileName = String.Empty;
			DocumentLog = null;
		}

		public DocumentForParsing(DocumentReceiveLog log)
		{
			DocumentLog = log;
			FileName = log.GetFileName();
		}

		public DocumentForParsing(DocumentReceiveLog log, string fileName)
		{
			DocumentLog = log;
			FileName = fileName;
		}

		public string FileName { get; set; }
		public DocumentReceiveLog DocumentLog { get; set;}
	}

	public class WaybillSourceHandler : EMAILSourceHandler
	{
		// Код клиента (аптеки)
		private uint? _clientId;

		// Типы документов (накладные, отказы)
		private readonly List<InboundDocumentType> _documentTypes;

		// Тип текущего документа (накладная или отказ)
		private InboundDocumentType _currentDocumentType;

		// Email, из которого будут браться накладные и отказы
		private string _imapUser = Settings.Default.WaybillIMAPUser;

		// Пароль для указанного email-а
		private string _imapPassword = Settings.Default.WaybillIMAPPass;

		public WaybillSourceHandler()
		{
			SourceType = "WAYBILL";
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

		public override void CheckMime(Mime m)
		{
			var emailList = new List<string>();

			_clientId = null;
			_currentDocumentType = null;

			// Получаем кол-во корректных адресов, т.е. отправленных 
			// на @waybills.analit.net или на @refused.analit.net			
			if(m.MainEntity.To != null)
				CorrectClientAddress(m.MainEntity.To, ref emailList);
			if(m.MainEntity.Cc != null)
				CorrectClientAddress(m.MainEntity.Cc, ref emailList);

			emailList  = emailList.ConvertAll(s => s.ToUpper()).Distinct().ToList();

			var correctAddresCount = emailList.Count;
			

			// Все хорошо, если кол-во вложений больше 0 и распознан только один адрес как корректный
			// Если не сопоставили с клиентом)
			if (correctAddresCount == 0)
			{
				throw new EMailSourceHandlerException("Не найден клиент.",
					Settings.Default.ResponseDocSubjectTemplateOnNonExistentClient,
					Settings.Default.ResponseDocBodyTemplateOnNonExistentClient);
			}
			if (correctAddresCount == 1 && m.Attachments.Length == 0)
			{
				throw new EMailSourceHandlerException("Письмо не содержит вложений.",
					Settings.Default.ResponseDocSubjectTemplateOnNothingAttachs,
					Settings.Default.ResponseDocBodyTemplateOnNothingAttachs);
			}
			if (correctAddresCount > 1)
			{
				throw new EMailSourceHandlerException("Письмо отправленно нескольким клиентам.",
					Settings.Default.ResponseDocSubjectTemplateOnMultiDomen,
					Settings.Default.ResponseDocBodyTemplateOnMultiDomen);
			}
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
					throw new EMailSourceHandlerException(String.Format("Письмо содержит вложение размером больше максимально допустимого значения ({0} Кб).",
							Settings.Default.MaxWaybillAttachmentSize),
						Settings.Default.ResponseDocSubjectTemplateOnMaxWaybillAttachment,
						String.Format(Settings.Default.ResponseDocBodyTemplateOnMaxWaybillAttachment,
							Settings.Default.MaxWaybillAttachmentSize));
				}
			}
		}

		/// <summary>
		/// Проверяет, существует ли клиент с указанным кодом.
		/// Также ищет указанный код среди адресов в таблице future.Addresses,
		/// поэтому можно сказать, что также проверяет адрес клиента на существование
		/// </summary>
		private bool ClientExists(uint checkClientCode)
		{
			var queryGetClientCode = String.Format(@"
SELECT Addr.Id
FROM Future.Addresses Addr
WHERE Addr.Id = {0}", checkClientCode);
			return With.Connection(c => {
				var clientCode = MySqlHelper.ExecuteScalar(c, queryGetClientCode);
				return (clientCode != null);
			});
		}

		/// <summary>
		/// Извлекает код клиента (или код адреса клиента) из email адреса,
		/// на который поставщик отправил накладную (или отказ)
		/// </summary>
		/// <returns>Если код извлечен и соответствует коду клиента 
		/// (или коду адреса), будет возвращен этот код. 
		/// Если код не удалось извлечь или он не найден ни среди кодов клиентов,
		/// ни среди кодов адресов, будет возвращен null</returns>
		private uint? GetClientCode(string emailAddress)
		{ 
			emailAddress = emailAddress.ToLower();
			InboundDocumentType testType = null;
			uint? testClientCode = null;

			foreach (var documentType in _documentTypes)
			{
				uint clientCode;

				// Пытаемся извлечь код клиента из email адреса
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
						_clientId = testClientCode;
					}
				}
				else
					testClientCode = null;
			}

			return testClientCode;
		}
		
		private int CorrectClientAddress(AddressList addressList, ref List<string> emailList)
		{
			uint? currentClientCode;
			var clientCodeCount = 0;

			// Пробегаемся по всем адресам TO и ищем адрес вида 
			// <\d+@waybills.analit.net> или <\d+@refused.analit.net>
			// Если таких адресов несколько, то считаем, что письмо ошибочное и не разбираем его дальше
			foreach(var mailbox in  addressList.Mailboxes)
			{
				currentClientCode = GetClientCode(GetCorrectEmailAddress(mailbox.EmailAddress));
				if (currentClientCode.HasValue)
				{					
					emailList.Add(GetCorrectEmailAddress(mailbox.EmailAddress));
					clientCodeCount++;
				}
			}
			return clientCodeCount;
		}

		/// <summary>
		/// Возвращает SQL запрос для выборки поставщиков и e-mail-ов, 
		/// с которых они могут отправлять накладные и отказы (для конкретной аптеки)
		/// </summary>
		protected override string GetSQLSources()
		{
			return @"
SELECT
	s.Id FirmCode,
	s.Name ShortName,
	r.Region RegionName,
	st.EMailFrom	
FROM
	Documents.Waybill_Sources st
	INNER JOIN future.suppliers s ON s.Id = st.FirmCode
	INNER JOIN farm.regions r ON r.RegionCode = s.HomeRegion
WHERE	
	st.SourceID = 1";
		}

		protected override void ErrorOnMessageProcessing(Mime m, AddressList from, EMailSourceHandlerException e)
		{
			try
			{
				if (String.IsNullOrEmpty(e.Body))
				{
					SendUnrecLetter(m, from, e);
					return;
				}

				var subject = e.Subject;
				var body = e.Body;
				var message = e.Message;

				var attachments = m.Attachments.Where(a => !String.IsNullOrEmpty(a.GetFilename())).Aggregate("", (s, a) => s + String.Format("\"{0}\"\r\n", a.GetFilename()));
				SendErrorLetterToProvider(
					from, 
					subject, 
					body, m);

				if (e is EmailFromUnregistredMail)
				{
					var ms = new MemoryStream(m.ToByteData());
					FailMailSend(m.MainEntity.Subject, from.ToAddressListString(),
						m.MainEntity.To.ToAddressListString(), m.MainEntity.Date, ms, attachments, e.Body);
				}

				var comment = String.Format(@"{0} 
Отправители     : {1}
Получатели      : {2}
Список вложений : 
{3}
Тема письма поставщику : {4}
Тело письма поставщику : 
{5}",
					message,
					from.ToAddressListString(),
					m.MainEntity.To.ToAddressListString(),
					attachments,
					subject, 
					body);

				WriteLog(_currentDocumentType.DocType,
					GetFirmCodeByFromList(from),
					_clientId,
					null,
					comment,
					currentUID);
			}
			catch (Exception exMatch)
			{
				_logger.Error("Не удалось отправить нераспознанное письмо", exMatch);
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

		protected override string GetFailMail()
		{
			return Settings.Default.DocumentFailMail;
		}

		protected override void SendUnrecLetter(Mime m, AddressList fromList, EMailSourceHandlerException e)
		{
			try
			{
				var attachments = m.Attachments.Where(a => !String.IsNullOrEmpty(a.GetFilename())).Aggregate("", (s, a) => s + String.Format("\"{0}\"\r\n", a.GetFilename()));
				var ms = new MemoryStream(m.ToByteData());
				FailMailSend(m.MainEntity.Subject, fromList.ToAddressListString(), 
					m.MainEntity.To.ToAddressListString(), m.MainEntity.Date, ms, attachments, e.Message);

				WriteLog(_currentDocumentType.DocType,
					GetFirmCodeByFromList(fromList),
					_clientId,
					null,
					String.Format(@"{0} 
Тема            : {1} 
Отправители     : {2}
Получатели      : {3}
Список вложений : 
{4}
", 
						e.Message, 
						m.MainEntity.Subject, 
						fromList.ToAddressListString(), 
						m.MainEntity.To.ToAddressListString(), 
						attachments),
						currentUID);
			}
			catch (Exception exMatch)
			{
				_logger.Error("Не удалось отправить нераспознанное письмо", exMatch);
			}
		}

		// Проверяет, что поставщик может работать и работает с данным клиентом.
		// supplierId - код поставщика
		// clientId - код клиента (или из "новой" или из "старой" реальности)
		private bool SupplierAvaliableForClient(ulong supplierId, ulong clientId)
		{
			var supplier = new MySqlParameter("?SupplierId", MySqlDbType.Int32);
			supplier.Value = supplierId;
			var client = new MySqlParameter("?ClientId", MySqlDbType.Int32);
			client.Value = clientId;

			return With.Connection(c => Convert.ToInt32(MySqlHelper.ExecuteScalar(c,@"
SELECT count(i.Id)
FROM future.clients
	JOIN Future.Suppliers as s ON s.Id = ?SupplierId
	JOIN usersettings.pricesdata as prices ON prices.FirmCode = s.Id AND prices.enabled = 1 AND prices.AgencyEnabled = 1
	JOIN future.intersection as i ON
		i.ClientId = clients.Id
		AND i.PriceId = prices.PriceCode
		AND i.AgencyEnabled = 1
		AND i.AvailableForClient = 1
WHERE clients.Id = ?ClientId
", supplier, client)) > 0);
		}

		private DataRow SelectWaybillSourceForClient(DataRow[] sources, uint? deliveryId)
		{
			var addressId = deliveryId;
			var clientId = GetClientIdByAddress(ref addressId) ?? deliveryId;
			DataRow result = null;
			var countAvaliableClients = 0;

			foreach (var dataRow in sources)
			{
				var supplierId = Convert.ToUInt64(dataRow[WaybillSourcesTable.colFirmCode]);
				if (SupplierAvaliableForClient(supplierId, Convert.ToUInt64(clientId)))
				{
					result = dataRow;
					countAvaliableClients++;
				}
			}
			return (countAvaliableClients == 1) ? result : null;
		}

		protected override void ProcessAttachs(Mime m, AddressList FromList)
		{
			//Один из аттачментов письма совпал с источником, иначе - письмо не распознано
			bool matched = false;

			/*
			 * В накладных письма обрабатываются немного по-другому: 
			 * письма обрабатываются относительно адреса отправителя
			 * и если такой отправитель найден в истониках, то все вложения 
			 * сохраняются относительно него.
			 * Если он не найден, то ничего не делаем.
			 */
			foreach (var mbFrom in FromList.Mailboxes)
			{
				var sources = dtSources.Select(String.Format("({0} like '*{1}*')",
					WaybillSourcesTable.colEMailFrom, mbFrom.EmailAddress));
				// Адрес отправителя должен быть только у одного поставщика, 
				// если получилось больше, то ищем поставщика, который доступен клиенту,
				// если таких нет или несколько, то это ошибка

				DataRow source;

				if (sources.Length > 1)
				{
					source = SelectWaybillSourceForClient(sources, _clientId);
					if (source == null)
						throw new Exception(String.Format(
							"На адрес \"{0}\" назначено несколько поставщиков. Определить какой из них работает с клиентом не удалось", mbFrom.EmailAddress));
				}
				else if (sources.Length == 0)
					continue;
				else
					source = sources.Single();

				var attachments = m.GetValidAttachements();
				//двойная очистка FileCleaner и Cleanup нужно оставить только одно
				//думаю Cleaner подходит лучше
				using (var cleaner = new FileCleaner())
				{
					var savedFiles = new List<string>();
					foreach (var entity in attachments)
					{
						SaveAttachement(entity);
						var correctArchive = CheckFile();
						matched = true;
						if (!correctArchive)
						{
							WriteLog(_currentDocumentType.DocType,
								Convert.ToUInt32(source[WaybillSourcesTable.colFirmCode]),
								_clientId, Path.GetFileName(CurrFileName),
								"Не удалось распаковать файл", currentUID);
							Cleanup();
							continue;
						}
						if (ArchiveHelper.IsArchive(CurrFileName))
						{
							var files = Directory.GetFiles(CurrFileName + ExtrDirSuffix +
								Path.DirectorySeparatorChar, "*.*", SearchOption.AllDirectories);
							savedFiles.AddRange(files);
							cleaner.Watch(files);
						}
						else
						{
							savedFiles.Add(CurrFileName);
							cleaner.Watch(CurrFileName);
						}
					}
					var logs = ProcessWaybillFile(savedFiles, source);
					WaybillService.ParseWaybills(logs);
				}
				Cleanup();
				source.Delete();
				dtSources.AcceptChanges();
			}//foreach (MailboxAddress mbFrom in FromList.Mailboxes)

			if (!matched)
				throw new EmailFromUnregistredMail(
					"Для данного E-mail не найден источник в таблице documents.waybill_sources",
					Settings.Default.ResponseDocSubjectTemplateOnUnknownProvider,
					Settings.Default.ResponseDocBodyTemplateOnUnknownProvider);
		}

		private uint? GetFirmCodeByFromList(AddressList FromList)
		{
			foreach (MailboxAddress address in FromList)
			{
				var firmCode = With.Connection(c => MySqlHelper.ExecuteScalar(
					c,
					String.Format(@"
SELECT w.FirmCode 
FROM documents.waybill_sources w 
WHERE w.EMailFrom LIKE '%{0}%' AND w.SourceID = 1", address.EmailAddress)));
				if (firmCode != null)
					return Convert.ToUInt32(firmCode);
			}
			return null;
		}

		protected List<DocumentReceiveLog> ProcessWaybillFile(IList<string> files, DataRow drCurrent)
		{
			var logs = new List<DocumentReceiveLog>();
			foreach (var archiveFile in files)
			{
				var extractedFiles = new[] { archiveFile };
				if (ArchiveHelper.IsArchive(archiveFile))
					extractedFiles = Directory.GetFiles(archiveFile + ExtrDirSuffix + Path.DirectorySeparatorChar, "*.*", SearchOption.AllDirectories);

				logs.AddRange(extractedFiles.Select(s => MoveWaybill(s, drCurrent)));
			}
			return logs;
		}

		protected DocumentReceiveLog MoveWaybill(string file, DataRow source)
		{
			_logger.InfoFormat("WaybillSourceHandler: обработка файла {0}", file);
			var addressId = _clientId;
			var clientId = GetClientIdByAddress(ref addressId);
			if (clientId == null)
			{
				clientId = _clientId;
				addressId = null;
			}

			var log = DocumentReceiveLog.Log(Convert.ToUInt32(source[WaybillSourcesTable.colFirmCode]),
				clientId,
				addressId,
				file,
				_currentDocumentType.DocType,
				currentUID);
			log.CopyDocumentToClientDirectory();
			return log;
		}

		private void WriteLog(DocType documentType, uint? firmCode, uint? clientCode,
			string fileName, string addition, int messageUid)
		{
			var addressId = clientCode;
			var clientId = GetClientIdByAddress(ref addressId);
			if (clientId == null)
			{
				clientId = clientCode;
				addressId = null;
			}

			DocumentReceiveLog.Log(firmCode, clientId, addressId, fileName, documentType, addition, messageUid);
		}
	}
}
