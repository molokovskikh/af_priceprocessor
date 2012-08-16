using System;
using Common.Tools;
using Inforoom.Downloader;
using LumiSoft.Net.Mime;

namespace Inforoom.PriceProcessor.Downloader
{
	public class MiniMailException : EMailSourceHandlerException
	{
		public MiniMailException(string message) : base(message)
		{
		}

		public MiniMailException(string message, ResponseTemplate template) : base(message)
		{
			Template = template;
		}

		public ResponseTemplate Template { get; set; }
	}

	/*
	 Здравствуйте! Ваше письмо с темой {0} неизвестный адрес {1} С уважением
	 */

	public class MiniMailOnUnknownProviderException : MiniMailException
	{
		public MiniMailOnUnknownProviderException(string message, string suppliersEmails) : base(message, ResponseTemplate.MiniMailOnUnknownProvider)
		{
			SuppliersEmails = suppliersEmails;
		}

		public override string GetBody(Mime mime)
		{
			return string.Format(MailTemplate.Body, mime.MainEntity.Subject, SuppliersEmails);
		}

		public string SuppliersEmails { get; set; }
	}

	/*
	 Здравствуйте! Ваше письмо с темой {0} не будет доставлено по причинам {1} С уважением
	 */

	public class MiniMailOnEmptyRecipientsException : MiniMailException
	{
		public MiniMailOnEmptyRecipientsException(string message, string causeList) : base(message, ResponseTemplate.MiniMailOnEmptyRecipients)
		{
			CauseList = causeList;
		}

		public override string GetBody(Mime mime)
		{
			return string.Format(MailTemplate.Body, mime.MainEntity.Subject, CauseList);
		}

		public string CauseList { get; set; }
	}

	/*
	 Здравствуйте! Ваше письмо с темой {0} имеет размер {1} а должно не более {2} С уважением
	 */

	public class MiniMailOnMaxMailSizeException : MiniMailException
	{
		public MiniMailOnMaxMailSizeException(string message) : base(message, ResponseTemplate.MiniMailOnMaxAttachment)
		{
		}

		public override string GetBody(Mime mime)
		{
			return string.Format(MailTemplate.Body, mime.MainEntity.Subject, mime.MailSize() / 1024.0 / 1024.0, Settings.Default.MaxMiniMailSize);
		}
	}

	/*
	 Здравствуйте! Ваше письмо с темой {0} имеет расширение {1} а должно {2} С уважением
	 */

	public class MiniMailOnAllowedExtensionsException : MiniMailException
	{
		public MiniMailOnAllowedExtensionsException(string message, string errorExtention, string allowedExtensions) : base(message, ResponseTemplate.MiniMailOnAllowedExtensions)
		{
			ErrorExtention = errorExtention;
			AllowedExtensions = allowedExtensions;
		}

		public override string GetBody(Mime mime)
		{
			return string.Format(MailTemplate.Body, mime.MainEntity.Subject, ErrorExtention, AllowedExtensions);
		}

		public string ErrorExtention { get; set; }

		public string AllowedExtensions { get; set; }
	}

	/*
	 Здравствуйте! Ваше письмо не содержит тему, тело и вложения С уважением
	 */

	public class MiniMailOnEmptyLetterException : MiniMailException
	{
		public MiniMailOnEmptyLetterException(string message) : base(message, ResponseTemplate.MiniMailOnEmptyLetter)
		{
		}

		public override string GetBody(Mime mime)
		{
			return MailTemplate.Body;
		}
	}
}