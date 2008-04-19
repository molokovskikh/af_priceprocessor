using System;
using System.Collections.Generic;
using System.Text;
using Inforoom.PriceProcessor.Properties;
using Inforoom.Logging;
using System.Threading;
using System.Net.Mail;

namespace Inforoom.PriceProcessor
{
	public abstract class AbstractHandler
	{
		/// <summary>
		/// Ссылка на рабочую нитку
		/// </summary>
		protected Thread tWork;

		/// <summary>
		/// Время "застоя" нитки
		/// </summary>
		protected int SleepTime;
		/// <summary>
		/// Время последнего "касания" обработчика
		/// </summary>
		protected DateTime lastPing;

		//Известные ошибки, которые не надо несколько раз отправлять
		protected List<string> knowErrors;

		public AbstractHandler()
		{
			knowErrors = new List<string>();

			tWork = new Thread(new ThreadStart(ThreadWork));
			SleepTime = Settings.Default.HandlerRequestInterval;
		}

		//Работает ли?
		public bool Worked
		{
			get
			{
				return DateTime.Now.Subtract(lastPing).TotalMinutes < Settings.Default.HandlerTimeout;
			}
		}

		//Запуск обработчика
		public abstract void StartWork();

		public abstract void StopWork();

		protected virtual void Ping()
		{
			lastPing = DateTime.Now;
		}

		//Перезапуск обработчика
		public void RestartWork()
		{
			SimpleLog.Log(this.GetType().Name, "Перезапуск обработчика");
			try
			{
				StopWork();
				Thread.Sleep(1000);
			}
			catch (Exception ex)
			{
				SimpleLog.Log(this.GetType().Name, "Ошибка при останове нитки обработчика : {0}", ex);
			}
			tWork = new Thread(new ThreadStart(ThreadWork));
			try
			{
				StartWork();
			}
			catch (Exception ex)
			{
				SimpleLog.Log(this.GetType().Name, "Ошибка при запуске нитки обработчика : {0}", ex);
			}
			SimpleLog.Log(this.GetType().Name, "Перезапустили обработчик");
		}

		//Нитка, в которой осуществляется работа обработчика источника
		protected void ThreadWork()
		{
			while (true)
			{
				try
				{
					ProcessData();
				}
				catch (ThreadAbortException)
				{ }
				catch (Exception ex)
				{
					LoggingToService(ex.ToString());
				}
				Ping();
				Sleeping();
			}
		}

		protected void Sleeping()
		{
			Thread.Sleep(SleepTime * 1000);
		}

		//Метод для обработки данных для каждого источника - свой
		protected abstract void ProcessData();

		protected void LoggingToService(string Addition)
		{
			SimpleLog.Log(this.GetType().Name + ".Error", Addition);
			if (!knowErrors.Contains(Addition))
				try
				{
					MailMessage mm = new MailMessage(Settings.Default.ServiceMail, Settings.Default.ServiceMail,
						"Ошибка в PriceProcessor",
						String.Format("Обработчик : {0}\n{1}", this.GetType().Name, Addition));
					SmtpClient sc = new SmtpClient(Settings.Default.SMTPHost);
					sc.Send(mm);
					knowErrors.Add(Addition);
				}
				catch
				{
				}
		}
	}
}
