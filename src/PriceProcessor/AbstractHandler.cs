using System;
using System.Collections.Generic;
using System.Text;
using Inforoom.PriceProcessor.Properties;
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

		protected readonly log4net.ILog _logger;

		public AbstractHandler()
		{
			_logger = log4net.LogManager.GetLogger(this.GetType());

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
		public virtual void StartWork()
		{
			tWork.Start();
		}

		public virtual void StopWork()
		{
			tWork.Abort();
		}

		protected virtual void Ping()
		{
			lastPing = DateTime.Now;
		}

		//Перезапуск обработчика
		public void RestartWork()
		{
			_logger.Info("Перезапуск обработчика");
			try
			{
				StopWork();
				Thread.Sleep(1000);
			}
			catch (Exception ex)
			{
				_logger.Error("Ошибка при останове нитки обработчика", ex);
			}
			tWork = new Thread(new ThreadStart(ThreadWork));
			try
			{
				StartWork();
			}
			catch (Exception ex)
			{
				_logger.Error("Ошибка при запуске нитки обработчика", ex);
			}
			_logger.Info("Перезапустили обработчик");
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
			_logger.ErrorFormat("Ошибка в нитке обработчика: {0}", Addition);
			if (!knowErrors.Contains(Addition))
				try
				{
					using (MailMessage mm = new MailMessage(
						Settings.Default.ServiceMail, 
						Settings.Default.ServiceMail,
						"Ошибка в PriceProcessor",
						String.Format("Обработчик : {0}\n{1}", this.GetType().Name, Addition)))
					{
						SmtpClient sc = new SmtpClient(Settings.Default.SMTPHost);
						sc.Send(mm);
					}
					knowErrors.Add(Addition);
				}
				catch(Exception ex)
				{
					_logger.Error("Не получилось отправить письмо с ошибкой", ex);
				}
		}
	}
}
