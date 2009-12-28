using System;
using System.Net.Mail;
using Inforoom.PriceProcessor.Properties;
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

		public static void SendFromFarmToService(string subject, string body)
		{
			Send(Settings.Default.FarmSystemEmail,
			     Settings.Default.ServiceMail,
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

		public static void SendFromServiceToService(string subject, string body)
		{
			Send(Settings.Default.ServiceMail,
			     Settings.Default.ServiceMail,
			     subject,
			     body);

		}
	}
}
