using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using Inforoom.Downloader;
using LumiSoft.Net.Mime;

namespace Inforoom.PriceProcessor.Helpers
{
	public static class MimeEntityExtentions
	{
		public static IEnumerable<MimeEntity> GetValidAttachements(this Mime mime)
		{
			return mime.Attachments.Where(m => !String.IsNullOrEmpty(GetFilename(m)) && m.Data != null);
		}

		public static IEnumerable<string> GetAttachmentFilenames(this Mime mime)
		{
			var result = new List<string>();
			var attachments = mime.GetValidAttachements();
			foreach (var entity in attachments)
				result.Add(entity.GetFilename());
			return result;
		}

		public static string GetFilename(this MimeEntity entity)
		{
			if (!String.IsNullOrEmpty(entity.ContentDisposition_FileName))
				return Path.GetFileName(FileHelper.NormalizeFileName(entity.ContentDisposition_FileName));
			if (!String.IsNullOrEmpty(entity.ContentType_Name))
				return Path.GetFileName(FileHelper.NormalizeFileName(entity.ContentType_Name));
			return null;
		}

		public static bool IsMailAddress(string address)
		{
			try {
				new MailAddress(address);
				return true;
			}
			catch (FormatException) {
				return false;
			}
		}

		public static AddressList GetAddressList(Mime m)
		{
			// Заполняем список адресов From
			var from = new AddressList();
			bool senderFound = false;

			// адрес из поля Sender, может быть не установлен
			string senderAddress = null;
			// Если поле установлено и адрес не пустой
			if ((m.MainEntity.Sender != null) &&
				!String.IsNullOrEmpty(m.MainEntity.Sender.EmailAddress)) {
				// получаем корректный адрес
				senderAddress = GetCorrectEmailAddress(m.MainEntity.Sender.EmailAddress);
				// Если адрес получился некорректным, то сбрасываем значение поля
				if (!IsMailAddress(senderAddress))
					senderAddress = null;
			}
			// Иногда список адресов оказывается пуст - СПАМ
			if (m.MainEntity.From != null) {
				foreach (var a in m.MainEntity.From.Mailboxes) {
					//Проверяем, что адрес что-то содержит
					if (!String.IsNullOrEmpty(a.EmailAddress)) {
						// получам корректный адрес
						var correctAddress = GetCorrectEmailAddress(a.EmailAddress);
						// Если после всех проверок адрес является EMail-адресом, то добавляем в список
						if (IsMailAddress(correctAddress)) {
							@from.Add(new MailboxAddress(correctAddress));
							if (!String.IsNullOrEmpty(senderAddress) &&
								senderAddress.Equals(correctAddress, StringComparison.OrdinalIgnoreCase))
								senderFound = true;
						}
					}
				}
			}

			if (!String.IsNullOrEmpty(senderAddress) && !senderFound)
				@from.Add(new MailboxAddress(senderAddress));

			// Иногда список адресов оказывается пуст - СПАМ,
			// в этом случае создаем пустую коллекцию, чтобы все было в порядке
			if (m.MainEntity.To == null)
				m.MainEntity.To = new AddressList();

			return @from;
		}

		public static string GetCorrectEmailAddress(string source)
		{
			return source.Replace("'", String.Empty).Trim();
		}

		public static string[] GetRecipients(this Mime mime)
		{
			var mainEntity = mime.MainEntity;
			var mailboxes = new[] { mainEntity.To, mainEntity.Cc, mainEntity.Bcc }
				.Where(l => l != null && l.Mailboxes != null)
				.SelectMany(l => l.Mailboxes)
				.ToList();

			var realTo = mainEntity.Header.Get("X-Real-To:");
			if (realTo != null) {
				var boxes = realTo.Where(r => r.Value != null).Select(r => {
						try {
							return MailboxAddress.Parse(r.Value);
						}
						catch {
							return null;
						}
					})
					.Where(m => m != null)
					.ToArray();
				mailboxes.AddRange(boxes);
			}

			return mailboxes.Select(a => GetCorrectEmailAddress(a.EmailAddress)).ToArray();
		}
	}
}