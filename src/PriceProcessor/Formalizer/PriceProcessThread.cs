using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Common.Tools;
using Inforoom.PriceProcessor.Formalizer.Core;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using log4net;
using Inforoom.PriceProcessor;
using MySql.Data.MySqlClient;
using NPOI.SS.Formula.Functions;
using FileHelper = Common.Tools.FileHelper;

namespace Inforoom.Formalizer
{
	public class FileHashItem
	{
		public string ErrorMessage = String.Empty;
		public int ErrorCount;
	}

	public enum FormResults
	{
		OK = 2,
		Warrning = 3,
		Error = 5
	}

	public enum PriceProcessState
	{
		None,
		Begin,
		GetConnection,
		GetLogConnection,
		CreateTempDirectory,
		CheckConnection,
		CallValidate,
		CallFormalize,
		FinalCopy,
		CloseConnection,
		FinalizeThread
	}

	public interface IPriceFormalizer
	{
		void Formalize();
		IList<string> GetAllNames();
		bool Downloaded { get; set; }
		string InputFileName { get; set; }

		FormLog Stat { get; }
		PriceFormalizationInfo Info { get; }
	}

	public class PriceProcessThread
	{
		private readonly Thread _thread;
		//время прерывания рабочей нитки
		private DateTime? _abortingTime;

		private readonly PriceProcessLogger _log;
		private readonly ILog _logger = LogManager.GetLogger(typeof(PriceProcessThread));

		private IPriceFormalizer _workPrice;

		public PriceProcessThread(PriceProcessItem item, string prevErrorMessage, bool runThread = true)
		{
			StartDate = DateTime.UtcNow;
			ProcessItem = item;
			ProcessState = PriceProcessState.None;

			_log = new PriceProcessLogger(prevErrorMessage, item);
			_thread = new Thread(ThreadWork);
			_thread.Name = String.Format("PPT{0}", _thread.ManagedThreadId);
			if (runThread)
				_thread.Start();
		}

		public bool FormalizeOK { get; private set; }

		/// <summary>
		/// Говорит о том, что формализация закончена. Корректно в том случае, если нитку не прибили сразу после запуска,
		/// иначе надо смотреть на ThreadState
		/// </summary>
		public bool FormalizeEnd { get; private set; }

		/// <summary>
		/// время начала формализации
		/// </summary>
		public DateTime StartDate { get; private set; }

		public bool ThreadIsAlive
		{
			get { return _thread.IsAlive; }
		}

		public ThreadState ThreadState
		{
			get { return _thread.ThreadState; }
		}

		public bool IsAbortingLong
		{
			get
			{
				return (
					(((_thread.ThreadState & ThreadState.AbortRequested) > 0) || ((_thread.ThreadState & ThreadState.Aborted) > 0))
						&& _abortingTime.HasValue
						&& (DateTime.UtcNow.Subtract(_abortingTime.Value).TotalSeconds > Settings.Default.AbortingThreadTimeout));
			}
		}

		/// <summary>
		/// останавливаем рабочую нитку и выставляем время останова, чтобы обрубить по таймауту
		/// </summary>
		public void AbortThread()
		{
			if (!_abortingTime.HasValue) {
				_thread.Abort();
				_abortingTime = DateTime.UtcNow;
			}
		}

		/// <summary>
		/// вызываем метод Interrupt у нитки, если она находится в состоянии AbortRequested и WaitSleepJoin
		/// </summary>
		public void InterruptThread()
		{
			_thread.Interrupt();
		}

		public void Join(int timeout = -1)
		{
			_thread.Join(timeout);
		}

		public string TID
		{
			get { return _thread.Name; }
		}

		public string CurrentErrorMessage
		{
			get { return _log.CurrentErrorMessage; }
		}

		/// <summary>
		/// Ссылка на обрабатываемый элемент
		/// </summary>
		public PriceProcessItem ProcessItem { get; private set; }

		public PriceProcessState ProcessState { get; private set; }

		/// <summary>
		/// Процедура формализации
		/// </summary>
		public void ThreadWork()
		{
			ProcessState = PriceProcessState.Begin;
			var allWorkTimeString = String.Empty;
			using (var cleaner = new FileCleaner())
				try {
					//имя файла для копирования в директорию Base выглядит как: <PriceItemID> + <оригинальное расширение файла>
					var outPriceFileName = Path.Combine(Settings.Default.BasePath,
						ProcessItem.PriceItemId + Path.GetExtension(ProcessItem.FilePath));
					//Используем идентификатор нитки в качестве названия временной папки
					var tempPath = Path.GetTempPath() + TID + "\\";
					//изменяем имя файла, что оно было без недопустимых символов ('_')
					var tempFileName = tempPath + ProcessItem.PriceItemId + Path.GetExtension(ProcessItem.FilePath);
					cleaner.Watch(tempFileName);
					_logger.DebugFormat("Запущена нитка на обработку файла : {0}", ProcessItem.FilePath);

					ProcessState = PriceProcessState.CreateTempDirectory;

					//Создаем директорию для временного файла и копируем туда файл
					if (Directory.Exists(tempPath))
						FileHelper.DeleteDir(tempPath);

					Directory.CreateDirectory(tempPath);

					var max = 4;
					for (var i = 1; i < max; i++) {
						try {
							ProcessState = PriceProcessState.CallValidate;
							_workPrice = PricesValidator.Validate(ProcessItem.FilePath, tempFileName, (uint)ProcessItem.PriceItemId);

							_workPrice.Downloaded = ProcessItem.Downloaded;
							_workPrice.InputFileName = ProcessItem.FilePath;

							ProcessState = PriceProcessState.CallFormalize;
							_workPrice.Formalize();

							_log.SuccesLog(_workPrice);
							break;
						}
						catch (MySqlException e) {
							//Duplicate entry '%s' for key %d
							//всего скорее это значит что одновременно формализовался прайс-лист с такими же синонимами, нужно повторить попытку
							if (e.Number == 1062) {
								_logger.WarnFormat(String.Format("Повторяю формализацию прайс-листа попытка {0} из {1}", i, max), e);
							}
						}
						catch (Exception e) {
							var warning =
								(e as WarningFormalizeException) ?? (e.InnerException as WarningFormalizeException);
							if (warning != null) {
								_log.WarningLog(warning, warning.Message);
								break;
							}
							else {
								throw;
							}
						}
						finally {
							var tsFormalize = DateTime.UtcNow.Subtract(StartDate);
							_log.FormSecs = Convert.ToInt64(tsFormalize.TotalSeconds);
							allWorkTimeString = tsFormalize.ToString();
						}
					}

					FormalizeOK = true;
					ProcessState = PriceProcessState.FinalCopy;
					//Если файл не скопируется, то из Inbound он не удалиться и будет попытка формализации еще раз
					if (File.Exists(tempFileName))
						File.Copy(tempFileName, outPriceFileName, true);
					else
						File.Copy(ProcessItem.FilePath, outPriceFileName, true);
					var ft = DateTime.UtcNow;
					File.SetCreationTimeUtc(outPriceFileName, ft);
					File.SetLastWriteTimeUtc(outPriceFileName, ft);
					File.SetLastAccessTimeUtc(outPriceFileName, ft);
				}
				catch (ThreadAbortException e) {
					_logger.Warn(Settings.Default.ThreadAbortError, e);
					_log.ErrodLog(_workPrice, new Exception(Settings.Default.ThreadAbortError));
				}
				catch (Exception e) {
					_logger.Error("Ошибка при формализации прайс листа", e);
					_log.ErrodLog(_workPrice, e);
				}
				finally {
					ProcessState = PriceProcessState.FinalizeThread;
					_logger.InfoFormat("Нитка завершила работу с прайсом {0}: {1}.", ProcessItem.FilePath, allWorkTimeString);
					FormalizeEnd = true;
				}
		}
	}
}