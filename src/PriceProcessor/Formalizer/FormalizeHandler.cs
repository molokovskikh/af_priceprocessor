using System;
using System.Collections.Generic;
using System.Threading;
using System.Net.Mail;
using Inforoom.Logging;
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
			tWork.Start();
		}

		public override void StopWork()
		{
			FSW.EnableRaisingEvents = false;
			FSW.Created -= FSEHOnCreate;
			tWork.Abort();

			Thread.Sleep(600);

			SimpleLog.Log(this.GetType().Name, "Попытка останова ниток");

			for (int i = pt.Count - 1; i >= 0; i--)
			{
				//Если нитка работает, то останавливаем ее
				if ((pt[i] as PriceProcessThread).WorkThread.IsAlive)
				{
					SimpleLog.Log(this.GetType().Name, "Попытка останова нитки {0}", (pt[i] as PriceProcessThread).TID);
					(pt[i] as PriceProcessThread).WorkThread.Abort();
					SimpleLog.Log(this.GetType().Name, "Останов нитки выполнен {0}", (pt[i] as PriceProcessThread).TID);
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
				SimpleLog.Log(this.GetType().Name + "OnFileCreate", "{0}", ex);
			}
		}

		private void AddPriceFileToList(string priceFile)
		{
			//Если файл имеет префикс "d", то значит он был закачан, поэтому он уже в очереди на обработку
			if (!Path.GetFileName(priceFile).StartsWith("d", StringComparison.OrdinalIgnoreCase))
			{
				ulong? priceCode, costCode, priceItemId;
				PricesValidator.CheckPriceItemId(Path.GetFileNameWithoutExtension(priceFile), out priceCode, out costCode, out priceItemId);
				if (priceCode.HasValue)
				{
					PriceProcessItem item = new PriceProcessItem(false, priceCode.Value, costCode, priceItemId.Value, priceFile);
					if (!PriceItemList.AddItem(item))
					{
						//todo: здесь не понятно, что надо делать, т.к. прайс-лист не добавили по причине скаченного нового. Сейчас удаляю
						try
						{
							FileHelper.FileDelete(priceFile);
						}
						catch (Exception ex)
						{
							SimpleLog.Log(this.GetType().Name, String.Format("Не получилось удалить файл для формализации {0}: {1}", priceFile, ex));
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
						SimpleLog.Log(this.GetType().Name, String.Format("Не получилось удалить неизвестный файл {0}: {1}", priceFile, ex));
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
			}

			//обновляем дату последнего отчета в лог
			if (DateTime.Now.Subtract(lastStatisticReport).TotalSeconds > statisticPeriodPerSecs)
				lastStatisticReport = DateTime.Now;

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
							SimpleLog.Log(this.GetType().Name, String.Format("Останавливаем нитку из-за дублирующего прайс-листа {0}", thread.TID));
							thread.WorkThread.Abort();
							SimpleLog.Log(this.GetType().Name, String.Format("Останов нитки успешно вызван {0}", thread.TID));
						}
						else
						{
							//если нет нитки на формализацию, то просто удаляем файл из папки
							//todo: не понятно, что делать если здесь будет удаляться скаченый файл
							try
							{
								FileHelper.FileDelete(item.FilePath);
							}
							catch (Exception ex)
							{
								SimpleLog.Log(this.GetType().Name, String.Format("Не получилось удалить дублирующий файл {0}: {1}", item.FilePath, ex));
							}
						}
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
				SimpleLog.Log((DateTime.Now.Subtract(lastStatisticReport).TotalSeconds > statisticPeriodPerSecs), this.GetType().Name, "PriceItemList.Count = {0}", PriceItemList.list.Count);

				//Первым проходом запускаем только загруженные прайс-листы
				ProcessItemList(PriceItemList.GetDownloadedItemList());

				//Если все загруженные прайс-листы в работе и кол-во рабочих ниток меньше максимального кол-ва, то добавляем еще перепроведенные прайс-листы
				if ((PriceItemList.GetDownloadedCount() == GetDownloadedProcessCount()) && (pt.Count < Settings.Default.MaxWorkThread))
					ProcessItemList(PriceItemList.list);
			}
		}

		private void ProcessItemList(List<PriceProcessItem> ProcessList)
		{
			int j = 0;
			while ((pt.Count < Settings.Default.MaxWorkThread) && (j < ProcessList.Count))
			{
				PriceProcessItem item = ProcessList[j];

				//не запущен ли он уже в работу?
				if (!PriceProcessExist(item))
				{
					//существует ли файл на диске?
					if (File.Exists(item.FilePath))
					{
						//Если разница между временем создания элемента в PriceItemList и текущим временем больше 5 секунд, то берем файл в обработку
						if (DateTime.UtcNow.Subtract(item.CreateTime).TotalSeconds > 5)
						{
							SimpleLog.Log(this.GetType().Name + ".AddPriceProcessThread", "Adding PriceProcessThread = {0}", item.FilePath);
							FileHashItem hashItem;
							if (htEMessages.Contains(item.FilePath))
								hashItem = (FileHashItem)htEMessages[item.FilePath];
							else
							{
								hashItem = new FileHashItem();
								htEMessages.Add(item.FilePath, hashItem);
							}
							pt.Add(new PriceProcessThread(item, hashItem.ErrorMessage));
							SimpleLog.Log(this.GetType().Name + ".AddPriceProcessThread", "Added PriceProcessThread = {0}", item.FilePath);
							j++;
						}
					}
					else
					{
						//удаляем элемент из списка
						ProcessList.Remove(item);
						//Если список, переданный в процедуру, не является PriceItemList.list, то надо удалить и из глобального списка
						if (ProcessList != PriceItemList.list)
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
					if (p.FormalizeEnd || !p.WorkThread.IsAlive)
					{
						DeleteProcessThread(p);
						pt.RemoveAt(i);
					}
					else
						if ((DateTime.UtcNow.Subtract(p.StartDate).TotalMinutes > Settings.Default.MaxLiveTime) && (p.WorkThread.ThreadState != System.Threading.ThreadState.AbortRequested))
						{
							SimpleLog.Log(this.GetType().Name, String.Format("Останавливаем нитку по сроку {0}: IsAlive = {1}   ThreadState = {2}  FormalizeEnd = {3}  StartDate = {4}", p.TID, p.WorkThread.IsAlive, p.WorkThread.ThreadState, p.FormalizeEnd, p.StartDate));
							p.WorkThread.Abort();
							SimpleLog.Log(this.GetType().Name, String.Format("Останов нитки успешно вызван {0}", p.TID));
						}
						else
						{
							statisticMessage += String.Format("{0} ID={4} IsAlive={1} ThreadState={2} FormalizeEnd={3}, ", Path.GetFileName(p.ProcessItem.FilePath), p.WorkThread.IsAlive, p.WorkThread.ThreadState, p.FormalizeEnd, p.TID);
						}
				}

				SimpleLog.Log((DateTime.Now.Subtract(lastStatisticReport).TotalSeconds > statisticPeriodPerSecs), this.GetType().Name, String.Format("Кол-во работающих нитей {0} : {1}", pt.Count, statisticMessage));
			}
		}

		/// <summary>
		/// удаляем нитку с формализацией
		/// </summary>
		/// <param name="p"></param>
		private void DeleteProcessThread(PriceProcessThread p)
		{
			SimpleLog.Log(this.GetType().Name, String.Format("Удаляем нитку {0}: IsAlive = {1}   ThreadState = {2}  FormalizeEnd = {3}", p.TID, p.WorkThread.IsAlive, p.WorkThread.ThreadState, p.FormalizeEnd));
			FileHashItem hi = (FileHashItem)htEMessages[p.ProcessItem.FilePath];
			string prevErrorMessage = hi.ErrorMessage;
			hi.ErrorMessage = p.CurrentErrorMessage;

			//если формализация завершилась успешно
			if (p.FormalizeOK)
				try
				{
					//удаляем файл
					FileHelper.FileDelete(p.ProcessItem.FilePath);
					//удаляем информацию о последних ошибках
					htEMessages.Remove(p.ProcessItem.FilePath);
					//удаляем из списка на обработку
					PriceItemList.list.Remove(p.ProcessItem);
				}
				catch (Exception e)
				{
					SimpleLog.Log(this.GetType().Name, String.Format("Не получилось удалить файл {0} после успешной формализации: {1}", p.ProcessItem.FilePath, e));
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
						SimpleLog.Log(this.GetType().Name, String.Format("Не получилось удалить файл {0} после останова дублирующейся нитки: {1}", p.ProcessItem.FilePath, e));
					}
				}
				else
				{
					hi.ErrorCount++;
					//Если превысили лимит ошибок и прайс-лист был переподложен, то удаляем его из списка, помещаем в ErrorFilesPath 
					//и отправляем уведомление
					if ((hi.ErrorCount > Settings.Default.MaxErrorCount) && (!p.ProcessItem.Downloaded))
					{
						SimpleLog.Log(this.GetType().Name, "Удаляем файл из-за большого кол-ва ошибок: FileName:{0} ErrorCount:{1} Downloaded:{2} ErrorMessage:{3} PriceItemId:{4}", p.ProcessItem.FilePath, hi.ErrorCount, p.ProcessItem.Downloaded, hi.ErrorMessage, p.ProcessItem.PriceItemId);
						try
						{
							PriceItemList.list.Remove(p.ProcessItem);
						}
						catch (Exception e)
						{
							SimpleLog.Log(this.GetType().Name, String.Format("Не удалось удалить файл из списка {0}: {1}", p.ProcessItem.FilePath, e));
						}
						try
						{
							if (File.Exists(FileHelper.NormalizeDir(Settings.Default.ErrorFilesPath) + Path.GetFileName(p.ProcessItem.FilePath)))
								File.Delete(FileHelper.NormalizeDir(Settings.Default.ErrorFilesPath) + Path.GetFileName(p.ProcessItem.FilePath));
							File.Move(p.ProcessItem.FilePath, FileHelper.NormalizeDir(Settings.Default.ErrorFilesPath) + Path.GetFileName(p.ProcessItem.FilePath));
						}
						catch (Exception e)
						{
							SimpleLog.Log(this.GetType().Name, String.Format("Не удалось переместить файл {0} в каталог {1}: {2}", p.ProcessItem.FilePath, Settings.Default.ErrorFilesPath, e));
						}
						try
						{
							MailMessage Message = new MailMessage(Settings.Default.FarmSystemEmail, Settings.Default.SMTPWarningList, "Ошибка формализации",
								String.Format(Settings.Default.MaxErrorsError, p.ProcessItem.FilePath, Settings.Default.ErrorFilesPath, Settings.Default.MaxErrorCount));
							Message.BodyEncoding = System.Text.Encoding.UTF8;
							SmtpClient Client = new SmtpClient(Settings.Default.SMTPHost);
							Client.Send(Message);
						}
						catch (Exception e)
						{
							SimpleLog.Log(this.GetType().Name, String.Format("Не удалось отправить сообщение: {0}", e));
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
							SimpleLog.Log(this.GetType().Name, String.Format("Не удалось переместить файл в конец списка {0}: {1}", p.ProcessItem.FilePath, e));
						}
				}
			}
		}

	}
}
