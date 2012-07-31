using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using Inforoom.Formalizer;
using System.Collections;
using Inforoom.PriceProcessor;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class FormalizeHandler : AbstractHandler
	{
		private readonly FileSystemWatcher FSW;

		//список с рабочими нитками формализации
		protected readonly List<PriceProcessThread> pt;

		//Время последнего статистического отчета 
		private DateTime lastStatisticReport = DateTime.Now.AddDays(-1);

		//Период статистического отчета в секундах
		private const int statisticPeriodPerSecs = 30;

		protected Hashtable _errorMessages;

		/// <summary>
		/// Время последней удачной формализации
		/// </summary>
		private DateTime? _lastFormalizationDate;

		/// <summary>
		/// Если установлен в true, то формализация не происходит и было отправлено уведомление об этом
		/// </summary>
		private bool _formalizationFail;

		public FormalizeHandler()
		{
			//Время паузы обработчика - 5 секунд
			SleepTime = 5;

			//Создали наблюдателя за файлами
			FSW = new FileSystemWatcher(Settings.Default.InboundPath, "*.*");
			pt = new List<PriceProcessThread>();
		}

		//Запуск обработчика
		public override void StartWork()
		{
			//Получили список файлов и добавил его на обраобтку
			foreach (var priceFile in Directory.GetFiles(Settings.Default.InboundPath))
			{
				_logger.InfoFormat("Загрузил файл {0} из очереди", priceFile);
				AddPriceFileToList(priceFile, false);
			}

			foreach (var priceProcessThread in PriceItemList.list)
			{
				_logger.InfoFormat("Файл в очереди {0}", priceProcessThread.FilePath);
			}

			_errorMessages = new Hashtable();

			FSW.Created += OnFileCreate;
			FSW.EnableRaisingEvents = true;

			base.StartWork();
		}

		public override void StopWork()
		{
			FSW.EnableRaisingEvents = false;
			FSW.Created -= OnFileCreate;

			base.StopWork();

			if (!tWork.Join(maxJoinTime))
				_logger.ErrorFormat("Рабочая нитка не остановилась за {0} миллисекунд.", maxJoinTime);

			Thread.Sleep(600);

			_logger.Info("Попытка останова ниток");

			//Сначала для всех ниток вызваем Abort,
			for (int i = pt.Count - 1; i >= 0; i--)
				//Если нитка работает, то останавливаем ее
				if (pt[i].ThreadIsAlive)
				{
					pt[i].AbortThread();
					_logger.InfoFormat("Вызвали Abort() для нитки {0}", pt[i].TID);
				}

			//а потом ждем их завершения
			for (int i = pt.Count - 1; i >= 0; i--)
			{
				
				if (!pt[i].ThreadIsAlive) 
					continue;

				//Если нитка работает, то ожидаем ее останов
				_logger.InfoFormat("Ожидаем останов нитки {0}", pt[i].TID);
				pt[i].AbortThread();
				int _currentWaitTime = 0;
				while ((_currentWaitTime < maxJoinTime) && ((pt[i].ThreadState & ThreadState.Stopped) == 0))
				{
					if ((pt[i].ThreadState & ThreadState.WaitSleepJoin) > 0)
						pt[i].InterruptThread();
					Thread.Sleep(1000);
					_currentWaitTime += 1000;
				}
				if ((pt[i].ThreadState & ThreadState.Stopped) > 0)
					_logger.InfoFormat("Останов нитки выполнен {0}", pt[i].TID);
				else
					_logger.InfoFormat("Нитка формализации {0} не остановилась за {1} миллисекунд.", pt[i].TID, maxJoinTime);
			}

			//удяляем пулл временных папок
			var _poolDirectories = Directory.GetDirectories(Path.GetTempPath(), "PPT*");
			foreach(var _deletingDirectory in _poolDirectories)
				if (Directory.Exists(_deletingDirectory))
					try
					{
						Directory.Delete(_deletingDirectory, true);
					}
					catch (Exception exception)
					{
						_logger.ErrorFormat("Не получилось удалить директорию {0}\r\n{1}", _deletingDirectory, exception);
					}
		}

		/// <summary>
		/// Поиск рабочей нити по элементу PriceProcessItem
		/// </summary>
		/// <param name="item">Ссылка на элемент PriceProcessItem, по которому осуществляетс поиск</param>
		/// <returns>Найденая нитка или null</returns>
		private PriceProcessThread FindByProcessItem(PriceProcessItem item)
		{
			return pt.Find(thread => (thread.ProcessItem == item));
		}

		private int GetDownloadedProcessCount()
		{
			var DownloadedProcessList = pt.FindAll(thread => (thread.ProcessItem.Downloaded));
			return DownloadedProcessList.Count;
		}

		/// <summary>
		/// Событие возникает при создании файла в папке
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		private void OnFileCreate(object source, FileSystemEventArgs e)
		{
			try
			{
				AddPriceFileToList(e.FullPath, true);
			}
			catch (Exception ex)
			{
				_logger.Error("Ошибка на OnFileCreate", ex);
			}
		}

		private void AddPriceFileToList(string priceFile, bool ignoreDownloaded)
		{
			//Если файл имеет префикс "d", то значит он был закачан, поэтому он уже в очереди на обработку
			if (ignoreDownloaded && PriceProcessItem.IsDownloadedPrice(priceFile)) 
				return;

			var item = PriceProcessItem.TryToLoadPriceProcessItem(priceFile);
			if (item != null)
			{
				if (!PriceItemList.AddItem(item))
				{
					//todo: здесь не понятно, что надо делать, т.к. прайс-лист не добавили по причине скаченного нового. Сейчас удаляю
					try
					{
						global::Common.Tools.FileHelper.FileDelete(priceFile);
					}
					catch (Exception ex)
					{
						_logger.ErrorFormat("Не получилось удалить файл для формализации {0}\r\n{1}", priceFile, ex);
					}
				}
			}
			else
			{
				LoggingToService(String.Format(Settings.Default.UnknownPriceError, Path.GetFileName(priceFile)));
				try
				{
					global::Common.Tools.FileHelper.FileDelete(priceFile);
				}
				catch (Exception ex)
				{
					_logger.ErrorFormat("Не получилось удалить неизвестный файл {0}\r\n{1}", priceFile, ex);
				}
			}
		}

		public override void ProcessData()
		{
			//накладываем lock на список во время обработки процедуры, что не проверять по два раза
			lock (PriceItemList.list)
			{
				DeleteDoubleItems();

				ProcessThreads();

				AddPriceProcessThread();

				CheckFormalizationFail();
			}

			//обновляем дату последнего отчета в лог
			if (DateTime.Now.Subtract(lastStatisticReport).TotalSeconds > statisticPeriodPerSecs)
				lastStatisticReport = DateTime.Now;

		}

		private void CheckFormalizationFail()
		{
			//Если нет файлов на формализацию, то сбрасываем дату последней удачной формализации
			if (PriceItemList.list.Count == 0)
				_lastFormalizationDate = null;

			if (!_formalizationFail && (PriceItemList.list.Count > 0)
				&& _lastFormalizationDate.HasValue
				&& (DateTime.UtcNow.Subtract(_lastFormalizationDate.Value).TotalMinutes > Settings.Default.MaxLiveTime))
			{

				var logMessage = String.Format(
					"Время последней удачной формализации = {0}\r\nОчередь на формализацию = {1}",
					_lastFormalizationDate.Value.ToLocalTime(),
					PriceItemList.list.Count);

				_logger.FatalFormat("Останов в нитках формализации.\r\n{0}", logMessage);
				Mailer.SendToWarningList("Останов в нитках формализации", logMessage);

				_formalizationFail = true;
			}
		}

		/// <summary>
		/// Удаляем дублирующиеся прайс-лист: переподложенные и два раза скаченные
		/// </summary>
		protected void DeleteDoubleItems()
		{ 
			int i = PriceItemList.list.Count-1;
			while(i > -1)
			{
				var item = PriceItemList.list[i];
				var downloadedItem = PriceItemList.GetLastestDownloaded(item.PriceItemId);
				if (downloadedItem == null || item == downloadedItem)
					//если нет скаченных прайс-листов, то элемент оставляем
					i--;
				else
				{
					//ищем элемент в рабочих нитках
					var thread = FindByProcessItem(item);
					if (thread != null)
					{
						//если элемент найден, то останавливаем нитку, файл будет удалять нитка при останове
						_logger.InfoFormat("Останавливаем нитку из-за дублирующего прайс-листа {0}", thread.TID);
						thread.AbortThread();
						_logger.InfoFormat("Останов нитки успешно вызван {0}", thread.TID);
					}
					else
					{
						//если нет нитки на формализацию, то просто удаляем файл из папки
						try
						{
							global::Common.Tools.FileHelper.FileDelete(item.FilePath);
						}
						catch (Exception ex)
						{
							_logger.ErrorFormat("Не получилось удалить дублирующий файл {0}\r\n{1}", item.FilePath, ex);
						}
					}
					///Из очереди на обработку файл элемент удаляется сразу, а если была рабочая нитка, 
					///то она удаляется в ProcessThreads, когда остановиться или ее останов принудительно прервут по таймауту
					PriceItemList.list.Remove(item);
					i--;
				}
			}
		}

		/// <summary>
		/// добавляем новые нитки на формализацию
		/// </summary>
		protected void AddPriceProcessThread()
		{ 
			lock (pt)
			{
				if (DateTime.Now.Subtract(lastStatisticReport).TotalSeconds > statisticPeriodPerSecs)
					_logger.InfoFormat("PriceItemList.Count = {0}", PriceItemList.list.Count);

				//Первым проходом запускаем только загруженные прайс-листы
				ProcessItemList(PriceItemList.GetDownloadedItemList());

				//Если все загруженные прайс-листы в работе и кол-во рабочих ниток меньше максимального кол-ва, то добавляем еще перепроведенные прайс-листы
				//Поставил <= потому, что могут быть загруженные прайс-листы, которые уже удалили из PriceItemList, но их рабочие нитки еще не остановлены и не удалены
				if ((PriceItemList.GetDownloadedCount() <= GetDownloadedProcessCount()) && (pt.Count < Settings.Default.MaxWorkThread))
					ProcessItemList(PriceItemList.list);
			}
		}

		private void ProcessItemList(IList<PriceProcessItem> processList)
		{
			processList
				.Where(i => !File.Exists(i.FilePath))
				.ToList()
				.ForEach(i => {
					//удаляем элемент из списка
					processList.Remove(i);
					//Если список, переданный в процедуру, не является PriceItemList.list, то надо удалить и из глобального списка
					if (processList != PriceItemList.list)
						PriceItemList.list.Remove(i);
				});

			foreach (var item in processList.TakeWhile(i => pt.Count < Settings.Default.MaxWorkThread).Where(i => i.IsReadyForProcessing(pt)))
			{
				_logger.InfoFormat("Adding PriceProcessThread = {0}", item.FilePath);
				FileHashItem hashItem;
				if (_errorMessages.Contains(item.FilePath))
					hashItem = (FileHashItem)_errorMessages[item.FilePath];
				else
				{
					hashItem = new FileHashItem();
					_errorMessages.Add(item.FilePath, hashItem);
				}
				pt.Add(new PriceProcessThread(item, hashItem.ErrorMessage));
				//если значение не было установлено, то скорей всего не было ниток на формализацию, 
				//поэтому устанавливаем время последней удачной формализации как текущее время
				if (!_lastFormalizationDate.HasValue)
					_lastFormalizationDate = DateTime.UtcNow;
				_logger.InfoFormat("Added PriceProcessThread = {0}", item.FilePath);
			}
		}

		/// <summary>
		/// прозводим обработку рабочих ниток
		/// </summary>
		protected void ProcessThreads()
		{
			lock (pt)
			{
				var statisticMessage = String.Empty;

				for (var i = pt.Count - 1; i >= 0; i--)
				{
					var p = pt[i];
					//Если нитка не работает, то удаляем ее
					if (p.FormalizeEnd || !p.ThreadIsAlive || ((p.ThreadState & ThreadState.Stopped) > 0))
					{
						DeleteProcessThread(p);
						pt.RemoveAt(i);
					}
					else
						//Остановка нитки по сроку, если она работает дольше, чем можно
						if ((DateTime.UtcNow.Subtract(p.StartDate).TotalMinutes > Settings.Default.MaxLiveTime) && ((p.ThreadState & ThreadState.AbortRequested) == 0))
						{
							_logger.InfoFormat(System.Globalization.CultureInfo.CurrentCulture,
								"Останавливаем нитку по сроку {0}: IsAlive = {1}   ThreadState = {2}  FormalizeEnd = {3}  StartDate = {4}  ProcessState = {5}", 
								p.TID, 
								p.ThreadIsAlive, 
								p.ThreadState, 
								p.FormalizeEnd, 
								p.StartDate.ToLocalTime(), 
								p.ProcessState);
							p.AbortThread();
							_logger.InfoFormat("Останов нитки успешно вызван {0}", p.TID);
						}
						else
							//Принудительно завершаем прерванную нитку, т.к. время останова превысило допустимый интервал ожидания
							if (p.IsAbortingLong)
							{
								_logger.InfoFormat("Принудительно завершаем прерванную нитку {0}.", p.TID);
								DeleteProcessThread(p);
								pt.RemoveAt(i);
							}
							else
								if (((p.ThreadState & ThreadState.AbortRequested) > 0) && ((p.ThreadState & ThreadState.WaitSleepJoin) > 0))
								{
									_logger.InfoFormat("Вызвали прерывание для нитки {0}.", p.TID);
									p.InterruptThread();
								}
								else
								{
									statisticMessage += String.Format(
										"{0} ID={1} IsAlive={2} StartDate={3} ThreadState={4} FormalizeEnd={5} ProcessState={6}, ",
										Path.GetFileName(p.ProcessItem.FilePath),
										p.TID,
										p.ThreadIsAlive,
										p.StartDate.ToLocalTime(),
										p.ThreadState,
										p.FormalizeEnd,
										p.ProcessState);
								}
				}

				if (DateTime.Now.Subtract(lastStatisticReport).TotalSeconds > statisticPeriodPerSecs)
					_logger.InfoFormat("Кол-во работающих нитей {0} : {1}", pt.Count, statisticMessage);
			}
		}

		/// <summary>
		/// удаляем нитку с формализацией
		/// </summary>
		/// <param name="p"></param>
		private void DeleteProcessThread(PriceProcessThread p)
		{
			_logger.InfoFormat("Удаляем нитку {0}: IsAlive = {1}   ThreadState = {2}  FormalizeEnd = {3}  ProcessState = {4}", p.TID, p.ThreadIsAlive, p.ThreadState, p.FormalizeEnd, p.ProcessState);
			//при перезапуске обработчика мы очищаем _errorMessages (хз зачем это нужно но пусть так)
			//в этом случае мы получим null reference
			//по этому и обвязка
			var hi = (FileHashItem)_errorMessages[p.ProcessItem.FilePath];
			if (hi != null)
				hi.ErrorMessage = p.CurrentErrorMessage;

			//если формализация завершилась успешно
			if (p.FormalizeOK)
				try
				{
					//устанавливаем время последней удачной формализации
					_lastFormalizationDate = DateTime.UtcNow;
					_formalizationFail = false;
					//удаляем файл
					global::Common.Tools.FileHelper.FileDelete(p.ProcessItem.FilePath);
					//удаляем информацию о последних ошибках
					_errorMessages.Remove(p.ProcessItem.FilePath);
					//удаляем из списка на обработку
					PriceItemList.list.Remove(p.ProcessItem);
				}
				catch (Exception e)
				{
					_logger.ErrorFormat("Не получилось удалить файл {0} после успешной формализации\r\n{1}", p.ProcessItem.FilePath, e);
				}
			else
			{
				//если элемента в списке не существует, то значит он был удален как дублирующийся, то просто удаляем оставшийся файл
				if (!PriceItemList.list.Contains(p.ProcessItem))
				{
					try
					{
						//удаляем файл
						global::Common.Tools.FileHelper.FileDelete(p.ProcessItem.FilePath);
						//удаляем информацию о последних ошибках
						_errorMessages.Remove(p.ProcessItem.FilePath);
					}
					catch (Exception e)
					{
						_logger.ErrorFormat("Не получилось удалить файл {0} после останова дублирующейся нитки\r\n{1}", p.ProcessItem.FilePath, e);
					}
				}
				else
				{
					if (hi != null)
						hi.ErrorCount++;
					//Если превысили лимит ошибок, то удаляем его из списка, помещаем в ErrorFilesPath 
					//и отправляем уведомление
					if (hi != null && hi.ErrorCount > Settings.Default.MaxErrorCount)
					{
						_logger.InfoFormat("Удаляем файл из-за большого кол-ва ошибок: FileName:{0} ErrorCount:{1} Downloaded:{2} ErrorMessage:{3} PriceItemId:{4}", p.ProcessItem.FilePath, hi.ErrorCount, p.ProcessItem.Downloaded, hi.ErrorMessage, p.ProcessItem.PriceItemId);
						try
						{
							PriceItemList.list.Remove(p.ProcessItem);
						}
						catch (Exception e)
						{
							_logger.ErrorFormat("Не удалось удалить файл из списка {0}\r\n{1}", p.ProcessItem.FilePath, e);
						}
						try
						{
							var file = Path.Combine(Settings.Default.ErrorFilesPath, Path.GetFileName(p.ProcessItem.FilePath));
							if (File.Exists(file))
								File.Delete(file);
							File.Move(p.ProcessItem.FilePath, file);
						}
						catch (Exception e)
						{
							_logger.ErrorFormat("Не удалось переместить файл {0} в каталог {1}\r\n{2}", p.ProcessItem.FilePath, Settings.Default.ErrorFilesPath, e);
						}
						Mailer.SendToWarningList("Ошибка формализации",
						                         String.Format(Settings.Default.MaxErrorsError,
						                                       p.ProcessItem.FilePath,
						                                       Settings.Default.ErrorFilesPath,
						                                       Settings.Default.MaxErrorCount));

						_errorMessages.Remove(p.ProcessItem.FilePath);
					}
					else
						try
						{
							PriceItemList.list.Remove(p.ProcessItem);
							PriceItemList.list.Add(p.ProcessItem);
						}
						catch (Exception e)
						{
							_logger.ErrorFormat("Не удалось переместить файл в конец списка {0}\r\n{1}", p.ProcessItem.FilePath, e);
						}
				}
			}
		}

	}
}
