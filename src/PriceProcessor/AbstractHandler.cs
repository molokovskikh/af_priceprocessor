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
		protected CancellationToken Cancellation;

		protected bool Stoped = true;

		private readonly CancellationTokenSource _cancellationSource;

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
		protected int JoinTimeout = 5000;

		protected readonly log4net.ILog _logger;

		/// <summary>
		/// временная директория для скачивания файлов (+ TempPath + 'Down' + SourceType)
		/// </summary>
		protected string DownHandlerPath;

		protected AbstractHandler()
		{
			_cancellationSource = new CancellationTokenSource();
			Cancellation = _cancellationSource.Token;

			_logger = log4net.LogManager.GetLogger(GetType());

			knowErrors = new List<string>();

			tWork = new Thread(ThreadWork);
			SleepTime = Settings.Default.HandlerRequestInterval;
		}

		//Работает ли?
		public bool Worked
		{
			get { return DateTime.Now.Subtract(lastPing).TotalMinutes < Settings.Default.HandlerTimeout; }
		}

		public void CreateDownHandlerPath()
		{
			DownHandlerPath = Path.Combine(Settings.Default.TempPath, GetType().Name);
			if (!Directory.Exists(DownHandlerPath))
				Directory.CreateDirectory(DownHandlerPath);
			DownHandlerPath += Path.DirectorySeparatorChar;
		}

		protected void Cleanup()
		{
			var cleanupDirs = Directory.GetDirectories(DownHandlerPath);
			foreach (var dir in cleanupDirs)
				try {
					if (_logger.IsDebugEnabled)
						_logger.DebugFormat("Попытка удалить директорию : {0}", dir);
					if (Directory.Exists(dir))
						Directory.Delete(dir, true);
					if (_logger.IsDebugEnabled)
						_logger.DebugFormat("Директория удалена : {0}", dir);
				}
				catch (Exception ex) {
					_logger.ErrorFormat("Ошибка при удалении директории {0}:\r\n{1}", dir, ex);
				}

			var cleanupFiles = Directory.GetFiles(DownHandlerPath);
			foreach (var cleanupFile in cleanupFiles)
				try {
					if (_logger.IsDebugEnabled)
						_logger.DebugFormat("Попытка удалить файл : {0}", cleanupFile);
					if (File.Exists(cleanupFile))
						File.Delete(cleanupFile);
					if (_logger.IsDebugEnabled)
						_logger.DebugFormat("Файл удален : {0}", cleanupFile);
				}
				catch (Exception ex) {
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

		public void SoftStop()
		{
			try {
				_cancellationSource.Cancel();
			}
			catch(Exception e) {
				_logger.Error(String.Format("Ошибка при отмене обработчика {0}", this), e);
			}
		}

		public virtual void HardStop()
		{
			if (!Stoped)
				tWork.Abort();
		}

		protected void Ping()
		{
			lastPing = DateTime.Now;
		}

		//Нитка, в которой осуществляется работа обработчика источника
		protected void ThreadWork()
		{
			Stoped = false;
			try {
				while (!Cancellation.IsCancellationRequested) {
					try {
						ProcessData();
					}
					catch (Exception ex) {
						Log(ex);
					}
					Ping();
					Wait();
				}
			}
			finally {
				Stoped = true;
			}
		}

		protected void Wait()
		{
			Cancellation.WaitHandle.WaitOne(SleepTime * 1000);
		}

		//Метод для обработки данных для каждого источника - свой
		public abstract void ProcessData();

		protected void Log(Exception e, string message = null)
		{
			if (knowErrors.Contains(e.ToString()) || e is ThreadAbortException || e is OperationCanceledException) {
				_logger.Warn(String.Format("Ошибка в обработчике {0}", this), e);
			}
			else {
				_logger.Error(String.Format("Ошибка в обработчике {0}", this), e);
				knowErrors.Add(e.ToString());
			}
		}
	}
}