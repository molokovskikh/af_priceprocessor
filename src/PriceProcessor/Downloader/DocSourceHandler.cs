using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Castle.Core;
using Common.MySql;
using Common.Tools;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.Mime;
using MySql.Data.MySqlClient;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace Inforoom.Downloader
{
	public class UserMailInfo
	{
		public uint UserId { get; set; }
		public string Mail { get; set; }
		public User User { get; set; }
	}

	public class DocSourceHandler : EMAILSourceHandler
	{

		// Email, из которого будут браться накладные и отказы
		private string _imapUser = Settings.Default.DocIMAPUser;

		// Пароль для указанного email-а
		private string _imapPassword = Settings.Default.DocIMAPPass;

		private List<UserMailInfo> _parsedInfos;

		public DocSourceHandler()
		{
			SourceType = "Doc";
		}

		public DocSourceHandler(string imapUser, string imapPassword)
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
			_parsedInfos = new List<UserMailInfo>();

			// Получаем кол-во корректных адресов, т.е. отправленных 
			// на @waybills.analit.net или на @refused.analit.net			
			if(m.MainEntity.To != null)
				CorrectUserAddress(m.MainEntity.To, ref _parsedInfos);
			if(m.MainEntity.Cc != null)
				CorrectUserAddress(m.MainEntity.Cc, ref _parsedInfos);

			//Оставляем только уникальные
			_parsedInfos = _parsedInfos.GroupBy(i => i.UserId).Select(g => g.First()).ToList();

			var correctUserCount = _parsedInfos.Count;

			// Все хорошо, если кол-во вложений больше 0 и распознан только один адрес как корректный
			// Если не сопоставили с клиентом)
			if (correctUserCount == 0)
			{
				throw new EMailSourceHandlerException("Не найден пользователь.",
					Settings.Default.ResponseDocSubjectTemplateOnNonExistentClient,
					Settings.Default.ResponseDocBodyTemplateOnNonExistentClient);
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

		public bool ParseEmail(string email, out uint userId)
		{
			userId = 0;
			int Index = email.IndexOf("@docs.analit.net");
			if (Index > -1)
			{
				if (uint.TryParse(email.Substring(0, Index), out userId))
				{
					return true;
				}
				return false;
			}
			return false;
		}

		/// <summary>
		/// Проверяет, существует ли пользовател с указанным кодом.
		/// Также ищет указанный код среди адресов в таблице future.Users,
		/// поэтому можно сказать, что также проверяет пользователя на существование
		/// </summary>
		private bool UserExists(uint checkUserId)
		{
			var queryGetUserId = String.Format(@"
SELECT Users.Id
FROM Future.Users 
WHERE Users.Id = {0}", checkUserId);
			return With.Connection(c => {
				var userId = MySqlHelper.ExecuteScalar(c, queryGetUserId);
				return (userId != null);
			});
		}

		/// <summary>
		/// Извлекает код пользователя (или код адреса клиента) из email адреса,
		/// на который поставщик отправил накладную (или отказ)
		/// </summary>
		/// <returns>Если код извлечен и соответствует коду клиента 
		/// (или коду адреса), будет возвращен этот код. 
		/// Если код не удалось извлечь или он не найден ни среди кодов клиентов,
		/// ни среди кодов адресов, будет возвращен null</returns>
		private uint? GetUserId(string emailAddress)
		{ 
			emailAddress = emailAddress.ToLower();

			uint? testUserId = null;

			uint userId;
			if (ParseEmail(emailAddress, out userId))
			{
				testUserId = userId;
			}


			if (testUserId.HasValue && UserExists(testUserId.Value))
			{
			}
			else
				testUserId = null;

			return testUserId;
		}
		
		private int CorrectUserAddress(AddressList addressList, ref List<UserMailInfo> parsedInfo)
		{
			uint? currentUserId;
			var userIdCount = 0;

			// Пробегаемся по всем адресам TO и ищем адрес вида 
			// <\d+@docs.analit.net> или <\d+@docs.analit.net>
			foreach(var mailbox in  addressList.Mailboxes) {
				var mail = GetCorrectEmailAddress(mailbox.EmailAddress);
				currentUserId = GetUserId(mail);
				if (currentUserId.HasValue)
				{					
					parsedInfo.Add(new UserMailInfo{UserId = currentUserId.Value, Mail = mail});
					userIdCount++;
				}
			}
			return userIdCount;
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
				if (e is EmailFromUnregistredMail)
				{
					subject = Settings.Default.ResponseDocSubjectTemplateOnUnknownProvider;
					body = Settings.Default.ResponseDocBodyTemplateOnUnknownProvider;
					message = "Для данного E-mail не найден источник в таблице documents.waybill_sources";
				}
				var attachments = m.Attachments.Where(a => !String.IsNullOrEmpty(a.GetFilename())).Aggregate("", (s, a) => s + String.Format("\"{0}\"\r\n", a.GetFilename()));

				//SendErrorLetterToProvider(
				//    from, 
				//    subject, 
				//    body, m);

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

				_logger.WarnFormat("Нераспознанное письмо: {0}", comment);
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
			}
			catch (Exception exMatch)
			{
				_logger.Error("Не удалось отправить нераспознанное письмо", exMatch);
			}
		}

		protected override void ProcessAttachs(Mime m, AddressList FromList)
		{
			//Один из аттачментов письма совпал с источником, иначе - письмо не распознано
			bool matched = false;

			var suppliers = GetSuppliersFromList(FromList.Mailboxes);

			if (suppliers.Count == 1) {
				matched = true;
				Cleanup();

				using (new SessionScope()) {
					_parsedInfos.ForEach(i => i.User = User.Find(i.UserId));
				}

				var mail = new Mail {
					Supplier = suppliers[0],
					Subject = m.MainEntity.Subject,
					Body = m.BodyText,
					LogTime = DateTime.Now
				};

			    var attachments = m.GetValidAttachements();
				foreach (var entity in attachments) {
				    SaveAttachement(entity);
					mail.Attachments.Add(new Attachment(mail, CurrFileName));
				}

				var mailLogs = _parsedInfos.Select(i => new MailSendLog {Mail = mail, User = i.User}).ToList();

				var attachmentLogs = new List<AttachmentSendLog>();
				foreach (var attachement in mail.Attachments) {
					attachmentLogs.AddRange(_parsedInfos.Select(i => new AttachmentSendLog{Attachment = attachement, User = i.User}));
				}

				using (var transaction = new TransactionScope(OnDispose.Rollback)) {
					mail.Create();
					mail.Attachments.ForEach(a =>
						File.Copy(
							a.LocalFileName,
							Path.Combine(Settings.Default.AttachmentPath, a.GetSaveFileName())));

					mailLogs.ForEach(l => l.Create());
					attachmentLogs.ForEach(l => l.Create());

					transaction.VoteCommit();
				}

			}
			else {
				throw new EmailFromUnregistredMail("Найдено несколько источников.");
			}

			if (!matched)
				throw new EmailFromUnregistredMail("Не найден источник.");
		}

		private List<Supplier> GetSuppliersFromList(MailboxAddress[] mailboxes)
		{
			var dtSuppliers = new DataTable();

			using(var connection = new MySqlConnection(Literals.ConnectionString()))
			{
				connection.Open();
				var adapter = new MySqlDataAdapter(@"
select
  s.Id
from
  future.Suppliers s
  inner join contacts.contact_groups cg on cg.ContactGroupOwnerId = s.ContactGroupOwnerId and cg.Type = 10
  inner join contacts.contacts c on c.ContactOwnerId = cg.Id and c.Type = 0
where
  c.ContactText in (" + mailboxes.Select(m => "'" + m.EmailAddress +"'").Implode() + ") group by s.Id", 
					  connection);
				adapter.Fill(dtSuppliers);
			}

			var result = new List<Supplier>();
			using (new SessionScope()) {
				foreach (DataRow dataRow in dtSuppliers.Rows) {
					result.Add(Supplier.Find(Convert.ToUInt32(dataRow["Id"])));
				}
			}

			return result;
		}

	}
}