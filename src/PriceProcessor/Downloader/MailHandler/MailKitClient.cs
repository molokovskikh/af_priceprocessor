using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading;
using Common.Tools;
using Inforoom.PriceProcessor.Helpers;
using log4net;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using NHibernate;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Inforoom.PriceProcessor.Downloader.MailHandler
{
	public partial class MailKitClient : AbstractHandler
	{
		private static ILog _log = LogManager.GetLogger(typeof (MailKitClient));

		private const string ExtrDirSuffix = "Extr";
		private const string ErrorLetterFrom = "tech@analit.net";
		public UniqueId CurrentMesdsageId { get; private set; }


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
			try {
				if (string.IsNullOrEmpty(Settings.Default.IMAPUrl)
					|| string.IsNullOrEmpty(Settings.Default.IMAPSourceFolder)
					|| string.IsNullOrEmpty(Settings.Default.WaybillIMAPUser)
					|| string.IsNullOrEmpty(Settings.Default.WaybillIMAPPass)
					|| string.IsNullOrEmpty(Settings.Default.WaybillImapMessageUser)
					|| string.IsNullOrEmpty(Settings.Default.WaybillImapMessagePass)
					|| string.IsNullOrEmpty(Settings.Default.IMAPHandlerErrorMessageTo)) {
					_log.Error(
						$"Для корректной работы обработчика {nameof(MailKitClient)} в файлах конфигурации необходимо задать слебудющие параметры: IMAPUrl, IMAPSourceFolder, WaybillIMAPUser, WaybillIMAPPass, WaybillImapMessageUser, WaybillImapMessagePass, IMAPHandlerErrorMessageTo.");
					return;
				}

				var imapFolderStr = Settings.Default.IMAPSourceFolder;
				var imapUrl = Settings.Default.IMAPUrl;

				var waybillImapUser = Settings.Default.WaybillIMAPUser;
				var waybillImapPass = Settings.Default.WaybillIMAPPass;

				GlobalContext.Properties["Version"] = Assembly.GetExecutingAssembly().GetName().Version;
				if (_log.IsDebugEnabled)
					_log.DebugFormat("Приложение запущено, конфигурация {0}", nameof(MailKitClient));
				var innerLogger = new MemorableLogger(_log) {
					ErrorMessage = $"Ошибка при обработке почтового ящика, конфигурация  {nameof(MailKitClient)}"
				};
				try {
					using (var client = new ImapClient()) {
						var credential = new NetworkCredential(waybillImapUser, waybillImapPass);
						var cancellation = default(CancellationToken);
						client.Connect(new Uri(imapUrl), cancellation);
						//что бы не спамить логи почтового сервера
						client.AuthenticationMechanisms.Clear();
						client.AuthenticationMechanisms.Add("PLAIN");
						client.Authenticate(credential, cancellation);
						var imapFolder = String.IsNullOrEmpty(imapFolderStr) ? client.Inbox : client.GetFolder(imapFolderStr);
						if (imapFolder == null) {
							var root = client.GetFolder(client.PersonalNamespaces[0]);
							imapFolder = root.Create(imapFolderStr, true, cancellation);
						}
						imapFolder.Open(FolderAccess.ReadWrite, cancellation);
						var ids = imapFolder.Search(SearchQuery.All, cancellation);
#if !DEBUG
						SessionHelper.StartSession(s => {
							using (var session = s.SessionFactory.OpenSession()) {
#endif
								foreach (var id in ids) {
									CurrentMesdsageId = id;
									var message = imapFolder.GetMessage(id, cancellation);
									try {
										ProcessMessage(session, message);
										imapFolder.SetFlags(id, MessageFlags.Deleted, true, cancellation);
									} catch (Exception e) {
										SendPublicErrorMessage($"Не удалось обработать письмо: при обработке письма возникла ошибка.", message);
										_log.Error($"Не удалось обработать письмо {message}", e);
									}
								}
#if !DEBUG
							}
						});
#endif
						imapFolder.Close(true, cancellation);
						client.Disconnect(true, cancellation);
					}
					innerLogger.Forget();
				} catch (Exception e) {
					innerLogger.Log(e);
				}
			} catch (Exception e) {
				_log.Error($"Ошибка при запуске приложения, конфигурация {nameof(MailKitClient)}", e);
			}
		}



		protected void SendPublicErrorMessage(string message, MimeMessage mimeMessage = null)
		{
			var imapHandlerErrorMessageTo = Settings.Default.IMAPHandlerErrorMessageTo;
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
			responseMime.To.Add(new MailboxAddress(imapHandlerErrorMessageTo, imapHandlerErrorMessageTo));
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
			var waybillImapUser = Settings.Default.WaybillImapMessageUser;
			var waybillImapPass = Settings.Default.WaybillImapMessagePass;

#if !DEBUG
			try {
				using (var client = new SmtpClient()) {
					var credential = new NetworkCredential(waybillImapUser, waybillImapPass);
					var cancellation = default(CancellationToken);
					client.Connect(imapUrl, 25, SecureSocketOptions.None); // EMAILSourceHandler -> SmtpClientEx.QuickSendSmartHost
					//что бы не спамить логи почтового сервера
					client.AuthenticationMechanisms.Clear();
					client.AuthenticationMechanisms.Add("PLAIN");
					client.Authenticate(credential, cancellation);

					client.Send(message);
					client.Disconnect(true, cancellation);
				}
			} catch (Exception e) {
				_log.Error($"Не удалось отправить письмо {message}", e);
			}
#endif
		}
	}
}