using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Web.Mail;
using System.Configuration;

namespace Inforoom.Formalizer
{
	/// <summary>
	/// Summary description for FormLog.
	/// </summary>
	public sealed class FormLog
	{
		private static StreamWriter log;
		private static ArrayList mess;

		static FormLog()
		{
			string logFileName = String.Empty;
			AppSettingsReader logSettings = new AppSettingsReader();			

			try
			{
				logFileName = (string)logSettings.GetValue("FormLog.FileName", typeof(string));
			}
			catch
			{
				logFileName = Path.ChangeExtension(System.Reflection.Assembly.GetExecutingAssembly().Location, ".log");
			}

			mess = new ArrayList();

			try
			{
				log = new StreamWriter(
					logFileName, 
					true, 
					System.Text.Encoding.GetEncoding(1251) 
					);

				Trace.Listeners.Add( new TextWriterTraceListener( log ) );
				Trace.AutoFlush = true;
				Trace.WriteLine("\n\n\n");
				Log("Log", "Started.");
			}
			catch(Exception ex)
			{
				try
				{
					MailMessage m = new MailMessage();
					m.From = FormalizeSettings.FromEmail;
					m.To = FormalizeSettings.RepEmail;
					m.Subject = "Error";
					m.BodyFormat = MailFormat.Text;
					m.BodyEncoding = System.Text.Encoding.GetEncoding(1251);
					m.Body = ex.ToString();
					SmtpMail.SmtpServer = "box.analit.net";
					SmtpMail.Send(m);
					mess.Add(ex.Message);
				}
				catch
				{
				}
			}
		}

		public static void Log(string SubSystem, string Message)
		{
			try
			{
				Trace.WriteLine( String.Format("{0}\t\t{1,-20}\t\t\t{2}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), SubSystem, Message) );
			}
			catch(Exception e)
			{
				if (!mess.Contains(e.Message))
				{
					try
					{
						MailMessage m = new MailMessage();
						m.From = FormalizeSettings.FromEmail;
						m.To = FormalizeSettings.RepEmail;
						m.Subject = "Error";
						m.BodyFormat = MailFormat.Text;
						m.BodyEncoding = System.Text.Encoding.GetEncoding(1251);
						m.Body = e.ToString();
						SmtpMail.SmtpServer = "box.analit.net";
						SmtpMail.Send(m);
						mess.Add(e.Message);
					}
					catch
					{
					}
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
