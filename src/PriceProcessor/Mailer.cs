using System;
using System.Net.Mail;
using Inforoom.PriceProcessor;
using log4net;

namespace Inforoom.PriceProcessor
{
	public class Mailer
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof (Mailer));

		public static void Send(MailMessage message)
		{
			try
			{
				var client = new SmtpClient(Settings.Default.SMTPHost);
				client.Send(message);
			}
			catch (Exception e)
			{
				_log.Error("Ошибка при отправке письма", e);
			}
		}

		public static void Send(string from, string to, string subject, string body)
		{
			Send(new MailMessage(from, to, subject, body));
		}

		public static void SendToWarningList(string subject, string body)
		{
			Send(Settings.Default.FarmSystemEmail,
			     Settings.Default.SMTPWarningList, 
				 subject, 
				 body);
		}

		public static void SendUserFail(string subject, string body)
		{
			Send(Settings.Default.ServiceMail,
			     Settings.Default.SMTPUserFail,
			     subject,
			     body);
		}
	}
}
