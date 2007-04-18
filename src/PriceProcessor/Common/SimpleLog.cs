using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Configuration;
using System.Net.Mail;

namespace Inforoom.Logging
{
	/// <summary>
	/// Summary description for SimpleLog
	/// </summary>
	public sealed class SimpleLog
	{
		private static StreamWriter log;
		private static ArrayList mess;

		private static string logFileName = String.Empty;

		private static string FromMail;
		private static string ToMail;

		//производить ли логирование
		private static bool Logging;

		static SimpleLog()
		{

			try
			{
				Logging = Convert.ToBoolean(ConfigurationManager.AppSettings["SimpleLog.Logging"]);
			}
			catch
			{
				Logging = false;
			}

			try
			{
				logFileName = ConfigurationManager.AppSettings["SimpleLog.FileName"];
				if (String.IsNullOrEmpty(logFileName))
					logFileName = Path.ChangeExtension(System.Reflection.Assembly.GetExecutingAssembly().Location, ".log");
			}
			catch
			{
				logFileName = Path.ChangeExtension(System.Reflection.Assembly.GetExecutingAssembly().Location, ".log");
			}

			try
			{
				FromMail = ConfigurationManager.AppSettings["SimpleLog.FromMail"];
			}
			catch
			{
				FromMail = "service@analit.net";
			}

			try
			{
				ToMail = ConfigurationManager.AppSettings["SimpleLog.ToMail"];
			}
			catch
			{
				ToMail = "service@analit.net";
			}

			mess = new ArrayList();

			if (Logging)
				try
				{
					log = new StreamWriter(
						logFileName, 
						true, 
						System.Text.Encoding.GetEncoding(1251) 
						);

					log.AutoFlush = true;
					log.WriteLine("\n\n\n");

					Log("Log", "Started.");
				}
				catch(Exception ex)
				{
					SendError(ex);
				}
		}

		private static void SendError(Exception ex)
		{
			try
			{
				MailMessage Message = new MailMessage(FromMail, ToMail, "Error on SimpleLog on " + logFileName,
					String.Format(
						"Процесс : {0}\n" +
						"Источник: {1}\n" +
						"Ошибка  : {2}",
						Process.GetCurrentProcess().MainModule.FileName,
						ex.Source,
						ex.ToString()));
				Message.BodyEncoding = System.Text.Encoding.UTF8;
				SmtpClient Client = new SmtpClient("box.analit.net");
				Client.Send(Message);
				mess.Add(ex.Message);
			}
			catch
			{
			}
		}

		public static void Log(string SubSystem, string Message)
		{
			try
			{
				if (Logging)
					log.WriteLine("{0}\t\t{1,-20}\t\t\t{2}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), SubSystem, Message);
			}
			catch(Exception e)
			{
				if (!mess.Contains(e.Message))
				{
					SendError(e);
				}
			}
		}

		public static void Log(string SubSystem, string Message, params object[] args)
		{
			Log(SubSystem, String.Format(Message, args) );
		}

		public static void Log(bool Condition, string SubSystem, string Message, params object[] args)
		{
			if (Condition) 
				Log(SubSystem, String.Format(Message, args) );
		}
	}
}
