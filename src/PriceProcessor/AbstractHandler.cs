using System;
using System.Collections.Generic;
using System.IO;
using Inforoom.PriceProcessor;
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

		/// <summary>
		/// кол-во милисекунд, которые выделяются на завершение нитки
		/// </summary>
		protected const int maxJoinTime = 5000; 

		protected readonly log4net.ILog _logger;

		/// <summary>
		/// временная директория для скачивания файлов (+ TempPath + 'Down' + SourceType)
		/// </summary>
		protected string DownHandlerPath;

		protected AbstractHandler()
		{
			_logger = log4net.LogManager.GetLogger(GetType());

			knowErrors = new List<string>();

			tWork = new Thread(ThreadWork);
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

		protected void CreateDownHandlerPath()
		{
			DownHandlerPath = Path.Combine(Settings.Default.TempPath, this.GetType().Name);
			if (!Directory.Exists(DownHandlerPath))
				Directory.CreateDirectory(DownHandlerPath);
			DownHandlerPath += Path.DirectorySeparatorChar;
		}

		protected void Cleanup()
		{
			var cleanupDirs = Directory.GetDirectories(DownHandlerPath);
			foreach (var dir in cleanupDirs)
				try
				{
					if (_logger.IsDebugEnabled)
						_logger.DebugFormat("Попытка удалить директорию : {0}", dir);
					if (Directory.Exists(dir))
						Directory.Delete(dir, true);
					if (_logger.IsDebugEnabled)
						_logger.DebugFormat("Директория удалена : {0}", dir);
				}
				catch (Exception ex)
				{
					_logger.ErrorFormat("Ошибка при удалении директории {0}:\r\n{1}", dir, ex);
				}

			var cleanupFiles = Directory.GetFiles(DownHandlerPath);
			foreach (var cleanupFile in cleanupFiles)
			try
			{

				if (_logger.IsDebugEnabled)
					_logger.DebugFormat("Попытка удалить файл : {0}", cleanupFile);
				if (File.Exists(cleanupFile))
					File.Delete(cleanupFile);
				if (_logger.IsDebugEnabled)
					_logger.DebugFormat("Файл удален : {0}", cleanupFile);
			}
			catch (Exception ex)
			{
				_logger.ErrorFormat("Ошибка при удалении файла {0}:\r\n{1}", cleanupFile, ex);
			}
		}

		//Запуск обработчика
		public virtual void StartWork()
		{
			CreateDownHandlerPath();
			tWork.Start();
			Ping();
		}

		public virtual void StopWork()
		{
			tWork.Abort();
		}

		protected void Ping()
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
			tWork = new Thread(ThreadWork);
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

		protected void LoggingToService(string addition)
		{
			_logger.ErrorFormat("Ошибка в обработчике {0}, {1}", GetType().Name, addition);
			if (knowErrors.Contains(addition))
				return;

			knowErrors.Add(addition);
			Mailer.SendFromServiceToService(
				String.Format("Ошибка в обработчике {0}", GetType().Name),
				addition);
		}
	}
}
