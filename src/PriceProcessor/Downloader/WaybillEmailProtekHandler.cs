using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Common.Tools;
using Dapper;
using Inforoom.Common;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Waybills.Rejects;
using NHibernate;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MailKit.Net.Smtp;
using MimeKit;
using NHibernate;

namespace Inforoom.PriceProcessor.Downloader
{
	public class WaybillEmailProtekHandler : AbstractHandler
	{
		private const string ExtrDirSuffix = "Extr";
		private const string ErrorLetterFrom = "tech@analit.net";
		private Settings settings;
		public UniqueId CurrentMesdsageId { get; private set; }

		public WaybillEmailProtekHandler()
		{
			settings = Settings.Default;
			if (string.IsNullOrWhiteSpace(settings.IMAPUrl)
				|| string.IsNullOrWhiteSpace(settings.IMAPSourceFolder)
				|| string.IsNullOrWhiteSpace(settings.MailKitClientUser)
				|| string.IsNullOrWhiteSpace(settings.MailKitClientPass)
				|| string.IsNullOrWhiteSpace(settings.IMAPHandlerErrorMessageTo)) {
				_logger.Error(
					$"Для корректной работы обработчика {nameof(WaybillEmailProtekHandler)} в файлах конфигурации необходимо задать слебудющие параметры: IMAPUrl, IMAPSourceFolder, MailKitClientUser, MailKitClientPass, IMAPHandlerErrorMessageTo.");
			}
		}

#if DEBUG
		protected ISession session;

		public void SetSessionForTest(ISession dbSession)
		{
			session = dbSession;
		}
#endif

		/// <summary>
		/// - Создание клиента, его запуск;
		/// - Загрузка сообщений;
		/// - Обработка сообщений;
		/// - Удаление сообщений;
		/// </summary>
		public override void ProcessData()
		{
			if (string.IsNullOrWhiteSpace(settings.IMAPUrl)
				|| string.IsNullOrWhiteSpace(settings.IMAPSourceFolder)
				|| string.IsNullOrWhiteSpace(settings.MailKitClientUser)
				|| string.IsNullOrWhiteSpace(settings.MailKitClientPass)
				|| string.IsNullOrWhiteSpace(settings.IMAPHandlerErrorMessageTo)) {
				return;
			}

			using (var client = new ImapClient()) {
				var credential = new NetworkCredential(settings.MailKitClientUser, settings.MailKitClientPass);
				client.Connect(new Uri(settings.IMAPUrl), Cancellation);
				//что бы не спамить логи почтового сервера
				client.AuthenticationMechanisms.Clear();
				client.AuthenticationMechanisms.Add("PLAIN");
				client.Authenticate(credential, Cancellation);
				var imapFolder = String.IsNullOrEmpty(settings.IMAPSourceFolder) ? client.Inbox : client.GetFolder(settings.IMAPSourceFolder);
				if (imapFolder == null) {
					var root = client.GetFolder(client.PersonalNamespaces[0]);
					imapFolder = root.Create(settings.IMAPSourceFolder, true, Cancellation);
				}
				imapFolder.Open(FolderAccess.ReadWrite, Cancellation);
				var ids = imapFolder.Search(SearchQuery.All, Cancellation);
#if !DEBUG
				using (var session = SessionHelper.GetSessionFactory().OpenSession()) {
#endif
					foreach (var id in ids) {
						CurrentMesdsageId = id;
						var message = imapFolder.GetMessage(id, Cancellation);
						try {
							ProcessMessage(session, message);
						} catch (Exception e) {
							NotifyAdmin($"Не удалось обработать письмо: при обработке письма возникла ошибка.", message);
							_logger.Error($"Не удалось обработать письмо {message}", e);
						} finally {
							imapFolder.SetFlags(id, MessageFlags.Deleted, true, Cancellation);
						}
					}
#if !DEBUG
				}
#endif
				imapFolder.Close(true, Cancellation);
				client.Disconnect(true, Cancellation);
			}
		}

		protected void NotifyAdmin(string message, MimeMessage mimeMessage = null)
		{
			_logger.Warn(message);
			var messageAppending = "";
			if (mimeMessage != null) {
				messageAppending = string.Format(@"

Отправитель:     {0};
Получатель:      {1};
Дата:            {2};
Заголовок:       {3};
Кол-во вложений: {4};
", string.Join(",", mimeMessage.From.Select(s => s.Name).ToList()),
					string.Join(",", mimeMessage.To.Mailboxes.Select(s => s.Address).ToList()),
					mimeMessage.Date,
					mimeMessage.Subject,
					mimeMessage.BodyParts.Count(s => s.IsAttachment));
			}
			var bodyBuilder = new BodyBuilder();
			bodyBuilder.TextBody = message + messageAppending;
			var responseMime = new MimeMessage();
			responseMime.From.Add(new MailboxAddress(ErrorLetterFrom, ErrorLetterFrom));
			responseMime.To.Add(new MailboxAddress(settings.IMAPHandlerErrorMessageTo, settings.IMAPHandlerErrorMessageTo));
			responseMime.Subject = "Ошбика в IMAP обработчике sst файлов.";
			responseMime.Body = bodyBuilder.ToMessageBody();

			var multipart = new Multipart("mixed");
			foreach (var item in responseMime.BodyParts) {
				multipart.Add(item);
			}

			if (mimeMessage != null)
				foreach (var item in mimeMessage.BodyParts) {
					multipart.Add(item);
				}

			responseMime.Body = multipart;

			SendMessage(responseMime);
		}

		//область видимости на уровне наследования для тестов
		protected virtual void SendMessage(MimeMessage message)
		{
			var imapUrl = Settings.Default.SMTPHost;
#if !DEBUG
			try {
				using (var client = new SmtpClient()) {
					client.Connect(imapUrl, 25, SecureSocketOptions.None);
					client.Send(message);
					client.Disconnect(true, Cancellation);
				}
			} catch (Exception e) {
				_logger.Error($"Не удалось отправить письмо {message}", e);
			}
#endif
		}

		public bool ProcessAttachments(ISession session, MimeMessage message, uint supplierId)
		{
			//Один из аттачментов письма совпал с источником, иначе - письмо не распознано
			var matched = false;

			var attachments = message.Attachments.Where(m => !String.IsNullOrEmpty(GetFileName(m)) && m.IsAttachment);
			if (!attachments.Any()) {
				NotifyAdmin($"Отсутствуют вложения в письме от адреса {message.To.Implode()}", mimeMessage: message);
			}

			using (var cleaner = new FileCleaner()) {
				foreach (var mimeEntity in attachments) {
					var savedFiles = new List<string>();
					//получение текущей директории
					var currentFileName = Path.Combine(DownHandlerPath, GetFileName(mimeEntity));

					//сохранение содержимого в текущую директорию
					using (var fs = new FileStream(currentFileName, FileMode.Create))
						((MimePart) mimeEntity).ContentObject.DecodeTo(fs);

					// нужно учесть, что файл может быть архивом
					var correctArchive = FileHelper.ProcessArchiveIfNeeded(currentFileName, ExtrDirSuffix);

					//если архив не распакован
					if (!correctArchive) {
						DocumentReceiveLog.Log(supplierId, null, Path.GetFileName(currentFileName), DocType.Waybill,
							"Не удалось распаковать файл", Convert.ToInt32(CurrentMesdsageId.Id));
						Cleanup();
						continue;
					}
					//если содержимое является архивом и он был распакован нужно извлеченные файлы добавить в список обрабатываемых (savedFiles) и отправить в обработчик мусора (в cleaner)
					if (ArchiveHelper.IsArchive(currentFileName)) {
						var files = Directory.GetFiles(currentFileName + ExtrDirSuffix +
							Path.DirectorySeparatorChar, "*.*", SearchOption.AllDirectories);
						savedFiles.AddRange(files);
						cleaner.Watch(files);
					} else {
						//только с расширения .sst
						if (new FileInfo(currentFileName).Extension.ToLower() == ".sst") {
							savedFiles.Add(currentFileName);
						}
						cleaner.Watch(currentFileName);
					}
					var logs = ProcessWaybillFile(session, savedFiles, supplierId, message);
					//если логи есть, значит файл распознан и найден соответствующий адрес доставки
					if (logs.Count > 0) {
						matched = true;
						var service = new WaybillService();
						service.Process(logs);
						if (service.Exceptions.Count > 0) {
							NotifyAdmin(service.Exceptions.First().Message, message);
						}
					}
				}
			}
			//удаление временных файлов
			Cleanup();
			return matched;
		}

		public static string GetFileName(MimeEntity entity)
		{
			if (!String.IsNullOrEmpty(entity.ContentDisposition.FileName))
				return Path.GetFileName(FileHelper.NormalizeFileName(entity.ContentDisposition.FileName));
			if (!String.IsNullOrEmpty(entity.ContentType.Name))
				return Path.GetFileName(FileHelper.NormalizeFileName(entity.ContentType.Name));
			return null;
		}

		private List<DocumentReceiveLog> ProcessWaybillFile(ISession session, IList<string> files, uint supplierId, MimeMessage mimeMessage)
		{
			var logs = new List<DocumentReceiveLog>();
			foreach (var archiveFile in files) {
				var extractedFiles = new[] {archiveFile};
				if (ArchiveHelper.IsArchive(archiveFile))
					extractedFiles = Directory.GetFiles(archiveFile + ExtrDirSuffix + Path.DirectorySeparatorChar, "*.*",
						SearchOption.AllDirectories);

				logs.AddRange(extractedFiles.Select(s => GetLog(session, s, supplierId, mimeMessage)).Where(l => l != null));
			}
			return logs;
		}

		private DocumentReceiveLog GetLog(ISession session, string file, uint supplierId, MimeMessage mimeMessage)
		{
			uint addressId = 0;
			Document doc = null;
			if (!string.IsNullOrEmpty(file) && new FileInfo(file)?.Extension?.ToLower() == ".sst") {
				doc = new WaybillSstParser().Parse(file, new Document());
			}
			if (doc == null) {
				//Распарсить, получить значение адреса, который связан с клиентом (необходимо для корректного проведения накладной)
				var parserType =
					new WaybillFormatDetector().GetSuitableParserByGroup(file, WaybillFormatDetector.SuitableParserGroups.Sst)
						.FirstOrDefault();
				if (parserType == null)
					return null;
				var constructor = parserType.GetConstructors().FirstOrDefault(c => c.GetParameters().Count() == 0);
				if (constructor == null)
					throw new Exception("Не найден парсер на этапе создание логов: у типа {0} нет конструктора без аргументов.");
				var parser = (IDocumentParser) constructor.Invoke(new object[0]);

				var document = new Document();
				doc = parser.Parse(file, document);
			}

			if (doc?.Invoice?.RecipientId == null) {
				NotifyAdmin($"В разобранном документе {file} не заполнено поле RecipientId для поставщика {supplierId}.", mimeMessage);
				return null;
			}
			var result = session.Connection.Query<uint?>(@"
				select ai.AddressId
				from Customers.Intersection i
				join Customers.AddressIntersection ai on ai.IntersectionId = i.Id
				join Usersettings.Pricesdata pd on pd.PriceCode = i.PriceId
				join Customers.Suppliers s on s.Id = pd.FirmCode
				where  ai.SupplierDeliveryId = @supplierDeliveryId
				 and s.Id  = @supplierId
				group by ai.AddressId", new {@supplierDeliveryId = doc.Invoice.RecipientId, @supplierId = supplierId})
				.FirstOrDefault();

			if (result.HasValue)
				addressId = result.Value;

			if (addressId == 0) {
				return null;
			}


			_logger.InfoFormat($"{nameof(WaybillEmailProtekHandler)}: обработка файла {file}");
			return DocumentReceiveLog.LogNoCommit(supplierId, addressId, file, DocType.Waybill, "Получен по Email",
				Convert.ToInt32(CurrentMesdsageId.Id));
		}

		private class SupplierSelector
		{
			public uint FirmCode { get; set; }
			public string EmailTo { get; set; }
		}

		public void ProcessMessage(ISession session, MimeMessage message)
		{
			//используется промежуточный почтовый ящик для транзита
			//в поле To будет именно он, этот же ящик используется для транзита прайс-листов
			var emails =
				message.To.OfType<MailboxAddress>().Where(s => !string.IsNullOrEmpty(s.Address)).Select(a => a.Address).ToArray();
			if (emails.Length == 0) {
				NotifyAdmin("У сообщения не указано ни одного получателя.", message);
				return;
			}

			var dtSources = session.Connection.Query<SupplierSelector>(@"
SELECT distinct
	s.Id as FirmCode,
	st.EMailTo
FROM farm.sourcetypes
	join farm.Sources as st on st.SourceTypeId = sourcetypes.Id
		join usersettings.PriceItems pi on pi.SourceId = st.ID
			join usersettings.PricesCosts pc on pc.PriceItemId = pi.ID
				join UserSettings.PricesData  as PD on PD.PriceCode = pc.PriceCode
					join Customers.Suppliers as s on s.Id = PD.FirmCode
WHERE
	sourcetypes.Type = 'EMail'
	and s.Disabled = 0
	and pd.AgencyEnabled = 1").ToList();

			var sources = emails
				.SelectMany(x => dtSources.Where(y => y.EmailTo?.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
				.Distinct()
				.ToList();

			if (sources.Count > 1) {
				// Нет адреса, клиента или другой информации об адресе доставки на этом этапе	//	SelectWaybillSourceForClient(sources, _addressId);
				NotifyAdmin(
					$"Для получателей {emails.Implode()} определено более одного поставщика." +
					$" Определенные поставщики {sources.Select(x => x.FirmCode).Implode()}.", message);
				return;
			}
			if (sources.Count == 0) {
				NotifyAdmin(
					$"Не удалось идентифицировать ни одного поставщика по адресам получателя {emails.Implode()}." +
					$" Количество записей в источниках - {dtSources.Count}", message);
				return;
			}

			var source = sources.First();
			//Один из аттачментов письма совпал с источником, иначе - письмо не распознано
			var matched = ProcessAttachments(session, message, source.FirmCode);
			if (!matched) {
				NotifyAdmin($"Для получателя {emails.Implode()} (поставщика {source.FirmCode}) не найден соответствующий адрес доставки.", message);
			}
		}
	}
}