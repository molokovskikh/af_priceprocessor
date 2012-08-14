using System;
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

	public class WaybillEmailSourceHandler : EMAILSourceHandler
	{
		// Код клиента (аптеки)
		private uint? _addressId;

		// Типы документов (накладные, отказы)
		private readonly List<InboundDocumentType> _documentTypes;

		// Тип текущего документа (накладная или отказ)
		private InboundDocumentType _currentDocumentType;

		// Email, из которого будут браться накладные и отказы
		private string _imapUser = Settings.Default.WaybillIMAPUser;

		// Пароль для указанного email-а
		private string _imapPassword = Settings.Default.WaybillIMAPPass;

		public WaybillEmailSourceHandler()
		{
			SourceType = "WAYBILL";
			_documentTypes = new List<InboundDocumentType> {
				new WaybillType(), new RejectType()
			};
		}

		public WaybillEmailSourceHandler(string imapUser, string imapPassword)
			: this()
		{
			if (!String.IsNullOrEmpty(imapUser) && !String.IsNullOrEmpty(imapPassword))
			{
				_imapUser = imapUser;
				_imapPassword = imapPassword;
			}
		}

		public override void IMAPAuth(IMAP_Client client)
		{
			client.Authenticate(_imapUser, _imapPassword);
		}

		public override void CheckMime(Mime m)
		{
			var emailList = new List<string>();

			_addressId = null;
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
			if (correctAddresCount == 0) {
				throw new EMailSourceHandlerException("Не найден клиент.",
					Settings.Default.ResponseDocSubjectTemplateOnNonExistentClient,
					Settings.Default.ResponseDocBodyTemplateOnNonExistentClient);
			}
			if (correctAddresCount == 1 && m.Attachments.Length == 0) {
				throw new EMailSourceHandlerException("Письмо не содержит вложений.",
					Settings.Default.ResponseDocSubjectTemplateOnNothingAttachs,
					Settings.Default.ResponseDocBodyTemplateOnNothingAttachs);
			}
			if (correctAddresCount > 1) {
				throw new EMailSourceHandlerException("Письмо отправленно нескольким клиентам.",
					Settings.Default.ResponseDocSubjectTemplateOnMultiDomen,
					Settings.Default.ResponseDocBodyTemplateOnMultiDomen);
			}
			if (m.Attachments.Length > 0) {
				var attachmentsIsBigger = m.Attachments.Any(attachment => (attachment.Data.Length/1024.0) > Settings.Default.MaxWaybillAttachmentSize);
				if (attachmentsIsBigger) {
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
		/// Также ищет указанный код среди адресов в таблице Customers.Addresses,
		/// поэтому можно сказать, что также проверяет адрес клиента на существование
		/// </summary>
		private bool ClientExists(uint checkClientCode)
		{
			var queryGetClientCode = String.Format(@"
SELECT Addr.Id
FROM Customers.Addresses Addr
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
						_addressId = testClientCode;
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
			foreach(var mailbox in  addressList.Mailboxes) {
				currentClientCode = GetClientCode(GetCorrectEmailAddress(mailbox.EmailAddress));
				if (currentClientCode.HasValue) {
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
	INNER JOIN Customers.suppliers s ON s.Id = st.FirmCode
	INNER JOIN farm.regions r ON r.RegionCode = s.HomeRegion
WHERE
	st.SourceID = 1";
		}

		protected override void ErrorOnMessageProcessing(Mime m, AddressList from, EMailSourceHandlerException e)
		{
			try
			{
				var subject = e.MailTemplate.Subject;
				var body = e.MailTemplate.Body;
				var message = e.Message;
				if (String.IsNullOrEmpty(body))
				{
					SendUnrecLetter(m, from, e);
					return;
				}

				var attachments = m.Attachments.Where(a => !String.IsNullOrEmpty(a.GetFilename())).Aggregate("", (s, a) => s + String.Format("\"{0}\"\r\n", a.GetFilename()));
				SendErrorLetterToSupplier(e, m);

				if (e is EmailFromUnregistredMail)
				{
					var ms = new MemoryStream(m.ToByteData());
					FailMailSend(m.MainEntity.Subject, from.ToAddressListString(),
						m.MainEntity.To.ToAddressListString(), m.MainEntity.Date, ms, attachments, body);
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

				DocumentReceiveLog.Log(GetFirmCodeByFromList(@from), _addressId, null, _currentDocumentType.DocType, comment, IMAPHandler.CurrentUID);
			}
			catch (Exception exMatch)
			{
				_logger.Error("Не удалось отправить нераспознанное письмо", exMatch);
			}
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

				DocumentReceiveLog.Log(GetFirmCodeByFromList(fromList), _addressId, null, _currentDocumentType.DocType, String.Format(@"{0}
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
					attachments), IMAPHandler.CurrentUID);
			}
			catch (Exception exMatch)
			{
				_logger.Error("Не удалось отправить нераспознанное письмо", exMatch);
			}
		}

		// Проверяет, что поставщик может работать и работает с данным клиентом.
		private bool SupplierAvaliableForClient(ulong supplierId, ulong addressId)
		{
			var supplier = new MySqlParameter("?SupplierId", MySqlDbType.Int32);
			supplier.Value = supplierId;
			var address = new MySqlParameter("?AddressId", MySqlDbType.Int32);
			address.Value = addressId;

			return With.Connection(c => Convert.ToInt32(MySqlHelper.ExecuteScalar(c,@"
SELECT count(i.Id)
FROM Customers.Addresses a
	JOIN Customers.Suppliers as s ON s.Id = ?SupplierId
	JOIN usersettings.pricesdata as prices ON prices.FirmCode = s.Id
	JOIN Customers.intersection as i ON i.ClientId = a.ClientId
		and i.LegalEntityId = a.LegalEntityId
		and i.PriceId = prices.PriceCode
		join Customers.AddressIntersection ai on ai.IntersectionId = i.Id and ai.AddressId = a.Id
WHERE a.Id = ?AddressId
	AND i.AgencyEnabled = 1
	AND i.AvailableForClient = 1
	AND prices.enabled = 1
	AND prices.AgencyEnabled = 1
", supplier, address)) > 0);
		}

		private DataRow SelectWaybillSourceForClient(DataRow[] sources, uint? addressId)
		{
			DataRow result = null;
			var countAvaliableClients = 0;

			foreach (var dataRow in sources) {
				var supplierId = Convert.ToUInt64(dataRow[WaybillSourcesTable.colFirmCode]);
				if (SupplierAvaliableForClient(supplierId, (addressId == null ? 0 : addressId.Value))) {
					result = dataRow;
					countAvaliableClients++;
				}
			}
			return (countAvaliableClients == 1) ? result : null;
		}

		protected override void ProcessAttachs(Mime mime, AddressList fromList)
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
			foreach (var mbFrom in fromList.Mailboxes) {
				var sources = dtSources.Select(String.Format("({0} like '*{1}*')",
					WaybillSourcesTable.colEMailFrom, mbFrom.EmailAddress));
				// Адрес отправителя должен быть только у одного поставщика,
				// если получилось больше, то ищем поставщика, который доступен клиенту,
				// если таких нет или несколько, то это ошибка

				DataRow source;

				if (sources.Length > 1) {
					source = SelectWaybillSourceForClient(sources, _addressId);
					if (source == null)
						throw new Exception(String.Format(
							"На адрес \"{0}\" назначено несколько поставщиков. Определить какой из них работает с клиентом не удалось", mbFrom.EmailAddress));
				}
				else if (sources.Length == 0)
					continue;
				else
					source = sources.Single();

				var attachments = mime.GetValidAttachements();
				//двойная очистка FileCleaner и Cleanup нужно оставить только одно
				//думаю Cleaner подходит лучше
				using (var cleaner = new FileCleaner()) {
					var savedFiles = new List<string>();
					foreach (var entity in attachments) {
						SaveAttachement(entity);
						var correctArchive = CheckFile();
						matched = true;
						if (!correctArchive) {
							DocumentReceiveLog.Log(Convert.ToUInt32(source[WaybillSourcesTable.colFirmCode]),
								_addressId,
								Path.GetFileName(CurrFileName),
								_currentDocumentType.DocType,
								"Не удалось распаковать файл",
								IMAPHandler.CurrentUID);
							Cleanup();
							continue;
						}
						if (ArchiveHelper.IsArchive(CurrFileName)) {
							var files = Directory.GetFiles(CurrFileName + ExtrDirSuffix +
								Path.DirectorySeparatorChar, "*.*", SearchOption.AllDirectories);
							savedFiles.AddRange(files);
							cleaner.Watch(files);
						}
						else {
							savedFiles.Add(CurrFileName);
							cleaner.Watch(CurrFileName);
						}
					}
					var logs = ProcessWaybillFile(savedFiles, source);

					var service = new WaybillService();
					service.Process(logs);
					if (service.Exceptions.Count > 0) {
						SendErrorLetterToSupplier(service.Exceptions.First(), mime);
					}
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
			foreach (var archiveFile in files) {
				var extractedFiles = new[] { archiveFile };
				if (ArchiveHelper.IsArchive(archiveFile))
					extractedFiles = Directory.GetFiles(archiveFile + ExtrDirSuffix + Path.DirectorySeparatorChar, "*.*", SearchOption.AllDirectories);

				logs.AddRange(extractedFiles.Select(s => GetLog(s, drCurrent)));
			}
			return logs;
		}

		protected DocumentReceiveLog GetLog(string file, DataRow source)
		{
			_logger.InfoFormat("WaybillEmailSourceHandler: обработка файла {0}", file);
			var addressId = _addressId;

			return DocumentReceiveLog.LogNoCommit(
				Convert.ToUInt32(source[WaybillSourcesTable.colFirmCode]),
				addressId,
				file,
				_currentDocumentType.DocType,
				"Получен по Email",
				IMAPHandler.CurrentUID);
		}
	}
}
