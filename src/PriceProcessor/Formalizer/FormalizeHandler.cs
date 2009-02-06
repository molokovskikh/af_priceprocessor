using System;
using System.Collections.Generic;
using System.Threading;
using System.Net.Mail;
using System.IO;
using Inforoom.Formalizer;
using System.Collections;
using Inforoom.PriceProcessor.Properties;
using Inforoom.Common;

namespace Inforoom.PriceProcessor.Formalizer
{
	class FormalizeHandler : AbstractHandler
	{
		private FileSystemWatcher FSW;
		private FileSystemEventHandler FSEHOnCreate;

		//список с рабочими нитками формализации
		private List<PriceProcessThread> pt;

		//Время последнего статистического отчета 
		private DateTime lastStatisticReport = DateTime.Now.AddDays(-1);

		//Период статистического отчета в секундах
		private int statisticPeriodPerSecs = 30;

		private Hashtable htEMessages;

		/// <summary>
		/// Время последней удачной формализации
		/// </summary>
		private DateTime? _lastFormalizationDate = null;

		/// <summary>
		/// Если установлен в true, то формализация не происходит и было отправлено уведомление об этом
		/// </summary>
		private bool _formalizationFail = false;

		public FormalizeHandler()
			: base()
		{
			//Время паузы обработчика - 5 секунд
			SleepTime = 5;

			//Создали наблюдателя за файлами
			FSW = new FileSystemWatcher(Settings.Default.InboundPath, "*.*");
			FSEHOnCreate = new FileSystemEventHandler(OnFileCreate);

			pt = new List<PriceProcessThread>();
		}

		//Запуск обработчика
		public override void StartWork()
		{
			//Получили список файдов и добавил его на обраобтку
			foreach (string priceFile in Directory.GetFiles(Settings.Default.InboundPath))
			{
				AddPriceFileToList(priceFile);
				//Если файл имеет префикс "d", то значит он был закачан в прошлый раз, поэтому сейчас с ним никак не удастся поработать
				if (Path.GetFileName(priceFile).StartsWith("d", StringComparison.OrdinalIgnoreCase))
					try
					{
						FileHelper.FileDelete(priceFile);
					} catch { }
			}

			htEMessages = new Hashtable();

			FSW.Created += FSEHOnCreate;
			FSW.EnableRaisingEvents = true;

			Ping();
			base.StartWork();
		}

		public override void StopWork()
		{
			FSW.EnableRaisingEvents = false;
			FSW.Created -= FSEHOnCreate;

			base.StopWork();

			Thread.Sleep(600);

			_logger.Info("Попытка останова ниток");

			for (int i = pt.Count - 1; i >= 0; i--)
			{
				//Если нитка работает, то останавливаем ее
				if ((pt[i] as PriceProcessThread).ThreadIsAlive)
				{
					_logger.InfoFormat("Попытка останова нитки {0}", (pt[i] as PriceProcessThread).TID);
					(pt[i] as PriceProcessThread).AbortThread();
					_logger.InfoFormat("Останов нитки выполнен {0}", (pt[i] as PriceProcessThread).TID);
				}
			}

		}

		/// <summary>
		/// Поиск рабочей нити по элементу PriceProcessItem
		/// </summary>
		/// <param name="item">Ссылка на элемент PriceProcessItem, по которому осуществляетс поиск</param>
		/// <returns>Найденая нитка или null</returns>
		private PriceProcessThread FindByProcessItem(PriceProcessItem item)
		{
			return pt.Find(delegate(PriceProcessThread thread) { return (thread.ProcessItem == item); });
		}

		private int GetDownloadedProcessCount()
		{
			List<PriceProcessThread> DownloadedProcessList = pt.FindAll(delegate(PriceProcessThread thread) { return (thread.ProcessItem.Downloaded); });
			return DownloadedProcessList.Count;
		}
		/// <summary>
		/// Работает ли определенная нитка?
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		private bool PriceProcessExist(PriceProcessItem item)
		{
			return (FindByProcessItem(item) != null);
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
				AddPriceFileToList(e.FullPath);
			}
			catch (Exception ex)
			{
				_logger.Error("Ошибка на OnFileCreate", ex);
			}
		}

		private void AddPriceFileToList(string priceFile)
		{
			//Если файл имеет префикс "d", то значит он был закачан, поэтому он уже в очереди на обработку
			if (!Path.GetFileName(priceFile).StartsWith("d", StringComparison.OrdinalIgnoreCase))
			{
				var item = PriceProcessItem.TryToLoadPriceProcessItem(priceFile);
				if (item != null)
				{
					if (!PriceItemList.AddItem(item))
					{
						//todo: здесь не понятно, что надо делать, т.к. прайс-лист не добавили по причине скаченного нового. Сейчас удаляю
						try
						{
							FileHelper.FileDelete(priceFile);
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
						FileHelper.FileDelete(priceFile);
					}
					catch (Exception ex)
					{
						_logger.ErrorFormat("Не получилось удалить неизвестный файл {0}\r\n{1}", priceFile, ex);
					}
				}
			}
		}

		protected override void ProcessData()
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
				try
				{
					string logMessage = String.Format(
						"Время последней удачной формализации = {0}\r\nОчередь на формализацию = {1}", 
						_lastFormalizationDate.Value.ToLocalTime(), 
						PriceItemList.list.Count);

					_logger.FatalFormat("Останов в нитках формализации.\r\n{0}", logMessage);

					using (MailMessage Message = new MailMessage(
						Settings.Default.FarmSystemEmail,
						Settings.Default.SMTPErrorList,
						"Останов в нитках формализации",
						logMessage))
					{
						SmtpClient Client = new SmtpClient(Settings.Default.SMTPHost);
						Client.Send(Message);
					}
					_formalizationFail = true;
				}
				catch (Exception e)
				{
					_logger.Error("Не удалось отправить сообщение", e);
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
				PriceProcessItem item = PriceItemList.list[i];
				PriceProcessItem downloadedItem = PriceItemList.GetLastestDownloaded(item.PriceItemId);
				if (downloadedItem == null)
					//если нет скаченных прайс-листов, то элемент оставляем
					i--;
				else
					if (item == downloadedItem)
						//если элемент - последний скаченный, то оставляем
						i--;
					else
					{
						//ищем элемент в рабочих нитках
						PriceProcessThread thread = FindByProcessItem(item);
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
								FileHelper.FileDelete(item.FilePath);
							}
							catch (Exception ex)
							{
								_logger.ErrorFormat("Не получилось удалить дублирующий файл {0}\r\n{1}", item.FilePath, ex);
							}
						}
						///Из очереди на обработку файл элемент удаляется сразу, а если была рабочая нитка, 
						///то она удаляется в ProcessThreads, когда остановиться или ее останов принудительно прервут по таймауту
						PriceItemList.list.Remove(item);
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
			int j = 0;
			while ((pt.Count < Settings.Default.MaxWorkThread) && (j < processList.Count))
			{
				PriceProcessItem item = processList[j];

				//не запущен ли он уже в работу?
				if (!PriceProcessExist(item))
				{
					//существует ли файл на диске?
					if (File.Exists(item.FilePath))
					{
						//Если разница между временем создания элемента в PriceItemList и текущим временем больше 5 секунд, то берем файл в обработку
						if (item.IsReadyForProcessing(pt))
						{
							_logger.InfoFormat("Adding PriceProcessThread = {0}", item.FilePath);
							FileHashItem hashItem;
							if (htEMessages.Contains(item.FilePath))
								hashItem = (FileHashItem)htEMessages[item.FilePath];
							else
							{
								hashItem = new FileHashItem();
								htEMessages.Add(item.FilePath, hashItem);
							}
							pt.Add(new PriceProcessThread(item, hashItem.ErrorMessage));
							//если значение не было установлено, то скорей всего не было ниток на формализацию, 
							//поэтому устанавливаем время последней удачной формализации как текущее время
							if (!_lastFormalizationDate.HasValue)
								_lastFormalizationDate = DateTime.UtcNow;
							_logger.InfoFormat("Added PriceProcessThread = {0}", item.FilePath);

							j++;
						}
					}
					else
					{
						//удаляем элемент из списка
						processList.Remove(item);
						//Если список, переданный в процедуру, не является PriceItemList.list, то надо удалить и из глобального списка
						if (processList != PriceItemList.list)
							PriceItemList.list.Remove(item);
					}
				}
				else
					j++;
			}
		}

		/// <summary>
		/// прозводим обработку рабочих ниток
		/// </summary>
		protected void ProcessThreads()
		{
			lock (pt)
			{
				string statisticMessage = String.Empty;

				for (int i = pt.Count - 1; i >= 0; i--)
				{
					PriceProcessThread p = (pt[i] as PriceProcessThread);
					//Если нитка не работает, то удаляем ее
					if (p.FormalizeEnd || !p.ThreadIsAlive || (p.ThreadState == ThreadState.Stopped))
					{
						DeleteProcessThread(p);
						pt.RemoveAt(i);
					}
					else
						//Остановка нитки по сроку, если она работает дольше, чем можно
						if ((DateTime.UtcNow.Subtract(p.StartDate).TotalMinutes > Settings.Default.MaxLiveTime) && (p.ThreadState != System.Threading.ThreadState.AbortRequested))
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
			FileHashItem hi = (FileHashItem)htEMessages[p.ProcessItem.FilePath];
			string prevErrorMessage = hi.ErrorMessage;
			hi.ErrorMessage = p.CurrentErrorMessage;

			//если формализация завершилась успешно
			if (p.FormalizeOK)
				try
				{
					//устанавливаем время последней удачной формализации
					_lastFormalizationDate = DateTime.UtcNow;
					_formalizationFail = false;
					//удаляем файл
					FileHelper.FileDelete(p.ProcessItem.FilePath);
					//удаляем информацию о последних ошибках
					htEMessages.Remove(p.ProcessItem.FilePath);
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
						FileHelper.FileDelete(p.ProcessItem.FilePath);
						//удаляем информацию о последних ошибках
						htEMessages.Remove(p.ProcessItem.FilePath);
					}
					catch (Exception e)
					{
						_logger.ErrorFormat("Не получилось удалить файл {0} после останова дублирующейся нитки\r\n{1}", p.ProcessItem.FilePath, e);
					}
				}
				else
				{
					hi.ErrorCount++;
					//Если превысили лимит ошибок и прайс-лист был переподложен, то удаляем его из списка, помещаем в ErrorFilesPath 
					//и отправляем уведомление
					if ((hi.ErrorCount > Settings.Default.MaxErrorCount) && (!p.ProcessItem.Downloaded))
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
							if (File.Exists(FileHelper.NormalizeDir(Settings.Default.ErrorFilesPath) + Path.GetFileName(p.ProcessItem.FilePath)))
								File.Delete(FileHelper.NormalizeDir(Settings.Default.ErrorFilesPath) + Path.GetFileName(p.ProcessItem.FilePath));
							File.Move(p.ProcessItem.FilePath, FileHelper.NormalizeDir(Settings.Default.ErrorFilesPath) + Path.GetFileName(p.ProcessItem.FilePath));
						}
						catch (Exception e)
						{
							_logger.ErrorFormat("Не удалось переместить файл {0} в каталог {1}\r\n{2}", p.ProcessItem.FilePath, Settings.Default.ErrorFilesPath, e);
						}
						try
						{
							using (MailMessage Message = new MailMessage(Settings.Default.FarmSystemEmail, Settings.Default.SMTPWarningList, "Ошибка формализации",
								String.Format(Settings.Default.MaxErrorsError, p.ProcessItem.FilePath, Settings.Default.ErrorFilesPath, Settings.Default.MaxErrorCount)))
							{
								SmtpClient Client = new SmtpClient(Settings.Default.SMTPHost);
								Client.Send(Message);
							}
						}
						catch (Exception e)
						{
							_logger.Error("Не удалось отправить сообщение", e);
						}
						htEMessages.Remove(p.ProcessItem.FilePath);
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
