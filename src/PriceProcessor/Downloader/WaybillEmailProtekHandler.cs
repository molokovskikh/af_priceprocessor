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

namespace Inforoom.PriceProcessor.Downloader
{
	public class WaybillEmailProtekHandler : AbstractHandler
	{
		private bool configured;
		private Settings settings;

		public WaybillEmailProtekHandler()
		{
			settings = Settings.Default;
			if (string.IsNullOrWhiteSpace(settings.IMAPUrl)
				|| string.IsNullOrWhiteSpace(settings.IMAPSourceFolder)
				|| string.IsNullOrWhiteSpace(settings.MailKitClientUser)
				|| string.IsNullOrWhiteSpace(settings.MailKitClientPass)
				|| string.IsNullOrWhiteSpace(settings.IMAPHandlerErrorMessageTo)) {
				_logger.Error(
					$"Для корректной работы обработчика {nameof(WaybillEmailProtekHandler)} в файлах конфигурации необходимо задать следующие параметры: IMAPUrl, IMAPSourceFolder, MailKitClientUser, MailKitClientPass, IMAPHandlerErrorMessageTo.");
			} else {
				configured = true;
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
			if (!configured)
				return;

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
						var message = imapFolder.GetMessage(id, Cancellation);
						try {
							ProcessMessage(session, message, id);
						} catch (Exception e) {
							_logger.Error($"Не удалось обработать письмо {message}", e);
						} finally {
							imapFolder.SetFlags(id, MessageFlags.Deleted, true, Cancellation);
						}
						Cleanup();
					}
#if !DEBUG
				}
#endif
				imapFolder.Close(true, Cancellation);
				client.Disconnect(true, Cancellation);
			}
		}

		protected void NotifyAdmin(string message, MimeMessage mimeMessage, Supplier supplier = null)
		{
			_logger.Warn(message);
			var messageAppending = "";
			if (mimeMessage != null) {
				messageAppending = string.Format($@"
Поставщик:       {supplier}
Отправитель:     {mimeMessage.From.Implode()};
Получатель:      {mimeMessage.To.Mailboxes.Implode()};
Дата:            {mimeMessage.Date};
Заголовок:       {mimeMessage.Subject};
Кол-во вложений: {mimeMessage.BodyParts.Count(s => s.IsAttachment)};");
			}
			var bodyBuilder = new BodyBuilder();
			bodyBuilder.TextBody = message + messageAppending;
			var responseMime = new MimeMessage();
			responseMime.From.Add(new MailboxAddress("tech@analit.net", "tech@analit.net"));
			responseMime.To.Add(new MailboxAddress(settings.IMAPHandlerErrorMessageTo, settings.IMAPHandlerErrorMessageTo));
			responseMime.Subject = "Ошибка обработки накладной от Протек.";
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

		public static string GetFileName(MimeEntity entity)
		{
			if (!String.IsNullOrEmpty(entity.ContentDisposition.FileName))
				return Path.GetFileName(global::Common.Tools.FileHelper.StringToFileName(entity.ContentDisposition.FileName));
			if (!String.IsNullOrEmpty(entity.ContentType.Name))
				return Path.GetFileName(global::Common.Tools.FileHelper.StringToFileName(entity.ContentType.Name));
			return null;
		}

		private class SupplierSelector
		{
			public uint FirmCode { get; set; }
			public string EmailTo { get; set; }
		}

		public void ProcessMessage(ISession session, MimeMessage message, UniqueId messageId = default(UniqueId))
		{
			//используется промежуточный почтовый ящик для транзита
			//в поле To будет именно он, этот же ящик используется для транзита прайс-листов
			var emails =
				message.To.OfType<MailboxAddress>().Where(s => !string.IsNullOrEmpty(s.Address)).Select(a => a.Address).ToArray();
			if (emails.Length == 0) {
				NotifyAdmin("У сообщения не указано ни одного получателя.", message);
				return;
			}
			var attachments = message.Attachments.Where(m => !String.IsNullOrEmpty(GetFileName(m)) && m.IsAttachment);
			if (!attachments.Any()) {
				NotifyAdmin($"Отсутствуют вложения в письме от адреса {message.To.Implode()}", message);
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
			var supplierId = source.FirmCode;
			var supplier = session.Load<Supplier>(supplierId);
			using (var cleaner = new FileCleaner()) {
				foreach (var mimeEntity in attachments) {
					//получение текущей директории
					var filename = Path.Combine(DownHandlerPath, GetFileName(mimeEntity));
					var files = new List<string> { filename };

					//сохранение содержимого в текущую директорию
					using (var fs = new FileStream(filename, FileMode.Create))
						((MimePart) mimeEntity).ContentObject.DecodeTo(fs);

					try {
						files = FileHelper.TryExtractArchive(filename, cleaner.RandomDir())?.ToList()
							?? files;
					} catch(ArchiveHelper.ArchiveException e) {
						_logger.Warn($"Не удалось распаковать файл {filename}", e);
						NotifyAdmin($"Не удалось распаковать файл {filename}.", message, supplier);
						continue;
					}

					var logs = new List<DocumentReceiveLog>();
					foreach (var file in files) {
						//нам нужно считать файл что бы узнать кто его отправил, по хорошему нам и не нужен пока что клиент
						var doc = new WaybillFormatDetector().Parse(session, file, new DocumentReceiveLog(supplier, new Address(new Client()), DocType.Waybill));
						if (doc == null) {
							NotifyAdmin($"Не удалось разобрать документ {file} нет подходящего формата.", message, supplier);
							continue;
						}
						if (doc.Invoice?.RecipientId == null) {
							if (doc.Parser == nameof(WaybillSstParser)) {
								NotifyAdmin($"В файле {file} не заполнено поле Код получателя.", message, supplier);
							} else {
								NotifyAdmin($"Формат файла {file} не соответствует согласованному формату sst. " +
									$"Поле 'Код получателя' не заполнено, возможно выбранный формат {doc.Parser} не считывает поле либо оно не заполнено в файла. " +
									$"Проверьте настройки формата {doc.Parser} и заполнение поля 'Код получателя' в файле.", message, supplier);
							}
							continue;
						}

						var result = session.Connection.Query<uint?>(@"
select ai.AddressId
from Customers.Intersection i
	join Customers.AddressIntersection ai on ai.IntersectionId = i.Id
	join Usersettings.Pricesdata pd on pd.PriceCode = i.PriceId
		join Customers.Suppliers s on s.Id = pd.FirmCode
where ai.SupplierDeliveryId = @supplierDeliveryId
	and s.Id  = @supplierId
group by ai.AddressId", new {@supplierDeliveryId = doc.Invoice.RecipientId, @supplierId = supplierId})
							.FirstOrDefault();

						if (result == null) {
							NotifyAdmin($"Не удалось обработать документ {file} для кода получателя {doc.Invoice.RecipientId} не найден адрес доставки. " +
								$"Проверьте заполнение поля 'Код адреса доставки' в личном кабинете поставщика {supplierId}.",
								message, supplier);
							continue;
						}

						_logger.InfoFormat($"Файл {file} обработан для кода получателя {doc.Invoice.RecipientId} выбран адрес {result.Value}");
						logs.Add(DocumentReceiveLog.LogNoCommit(supplierId, result.Value, file, DocType.Waybill, "Получен по Email",
							(int?)messageId.Id));
					}
					//если логи есть, значит файл распознан и найден соответствующий адрес доставки
					if (logs.Count > 0) {
						var service = new WaybillService();
						service.Process(logs);
						if (service.Exceptions.Count > 0) {
							NotifyAdmin(service.Exceptions.First().Message, message, supplier);
						}
					}
				}
			}
		}
	}
}