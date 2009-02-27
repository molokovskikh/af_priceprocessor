using System;
using System.IO;
using System.Threading;
using System.Net.Mail;
using MySql.Data.MySqlClient;
using Inforoom.PriceProcessor.Properties;
using System.Configuration;
using Inforoom.Common;
using Inforoom.PriceProcessor;
using System.Data;
using System.Reflection;
using FileHelper=Inforoom.Common.FileHelper;


namespace Inforoom.Formalizer
{

	public class FileHashItem
	{
		public string ErrorMessage = String.Empty;
		public int ErrorCount = 0;
	}

	enum FormResults : int
	{ 
		OK = 2,
		Warrning = 3,
		Error = 5
	}

	public enum PriceProcessState : int
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

	/// <summary>
	/// Summary description for PriceProcessThread.
	/// </summary>
	public class PriceProcessThread
	{
		//Если установлено, то письмо в ErrorList о проблемах с connection отправлено
		private static bool LetterAboutConnectionSended;

		//Временный каталог для файла
		private string TempPath;
		//Имя файла, который реально проходит обработку
		private string TempFileName;

		/// <summary>
		/// Ссылка на обрабатываемый элемент
		/// </summary>
		private readonly PriceProcessItem _processItem;

		//Соединение с базой
		private MySqlConnection myconn;
		private MySqlConnection _logConnection;
		private MySqlCommand mcLog;

		//Собственно нитка
		private readonly Thread _thread;
		//время прерывания рабочей нитки 
		private DateTime? _abortingTime;

		//Время начала формализации
		private readonly DateTime tmFormalize;
		//Время формализации в секундах
		private Int64 formSecs;

		//Результат формализации: Хорошо, Плохо

		//предыдущее сообщение об ошибке
		private readonly string prevErrorMessage = String.Empty;
		//текущее сообщение об ошибке
		private string currentErrorMessage = String.Empty; 

		private BasePriceParser WorkPrice;

		private readonly log4net.ILog _logger = log4net.LogManager.GetLogger(typeof(PriceProcessThread));

		private PriceProcessState _processState = PriceProcessState.None;

		public PriceProcessThread(PriceProcessItem item, string PrevErrorMessage)
		{
			tmFormalize = DateTime.UtcNow;
			prevErrorMessage = PrevErrorMessage;
			_processItem = item;
			_thread = new Thread(ThreadWork);
			_thread.Name = String.Format("PPT{0}", _thread.ManagedThreadId);
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
		public DateTime StartDate
		{
			get
			{
				return tmFormalize;
			}
		}

		public bool ThreadIsAlive
		{
			get
			{
				return _thread.IsAlive;
			}
		}

		public ThreadState ThreadState
		{
			get
			{
				return _thread.ThreadState;
			}
		}

		public bool IsAbortingLong
		{
			get
			{
				return (
					((_thread.ThreadState == ThreadState.AbortRequested) || (_thread.ThreadState == ThreadState.Aborted)) 
					&& _abortingTime.HasValue 
					&& (DateTime.UtcNow.Subtract(_abortingTime.Value).TotalSeconds > Settings.Default.AbortingThreadTimeout));
			}
		}

		/// <summary>
		/// останавливаем рабочую нитку и выставляем время останова, чтобы обрубить по таймауту
		/// </summary>
		public void AbortThread()
		{
			if (!_abortingTime.HasValue)
			{
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

		public string TID
		{
			get
			{
				return _thread.Name;
			}
		}

		public string CurrentErrorMessage
		{
			get
			{
				return currentErrorMessage;
			}
		}

		/// <summary>
		/// Ссылка на обрабатываемый элемент
		/// </summary>
		public PriceProcessItem ProcessItem
		{
			get
			{
				return _processItem;
			}
		}

		public PriceProcessState ProcessState
		{
			get 
			{
				return _processState;
			}
		}

		private void SuccesLog(BasePriceParser p)
		{
			string messageBody = "", messageSubject = "";
			//Формирование заголовков письма и 
			if (null == p)
			{
				SuccesGetBody("Прайс упешно формализован", ref messageSubject, ref messageBody, -1, -1, null);
			}
			else
			{
				SuccesGetBody("Прайс упешно формализован", ref messageSubject, ref messageBody, p.priceCode, p.firmCode, String.Format("{0} ({1})", p.firmShortName, p.priceName) );
			}


			if (null != mcLog)
			{
				try
				{
					try
					{
						if (_logConnection.State == ConnectionState.Open)
							_logConnection.Close();
					}
					catch(Exception onLogCloseException)
					{
						_logger.Error("Ошибка при закрытии лог-соединения", onLogCloseException);
					}

					_logConnection.Open();
					try
					{
						mcLog.CommandText = "INSERT INTO logs.FormLogs (LogTime, Host, PriceItemId, Form, Unform, Zero, Forb, ResultId, TotalSecs) VALUES (NOW(), ?Host, ?PriceItemId, ?Form, ?Unform, ?Zero, ?Forb, ?ResultId, ?TotalSecs )";
						mcLog.Parameters.Clear();
						mcLog.Parameters.AddWithValue("?Host", Environment.MachineName);
						mcLog.Parameters.AddWithValue("?PriceItemId", _processItem.PriceItemId);
						mcLog.Parameters.AddWithValue("?Form", p.formCount);
						mcLog.Parameters.AddWithValue("?Unform", p.unformCount);
						mcLog.Parameters.AddWithValue("?Zero", p.zeroCount);
						mcLog.Parameters.AddWithValue("?Forb", p.forbCount);
						mcLog.Parameters.AddWithValue("?ResultId", (p.maxLockCount <= Settings.Default.MinRepeatTranCount) ? FormResults.OK : FormResults.Warrning);
						mcLog.Parameters.AddWithValue("?TotalSecs", formSecs);					
						mcLog.ExecuteNonQuery();
					}
					finally
					{
						_logConnection.Close();
					}
				}
				catch(Exception e)
				{
					_logger.Error("Ошибка логирования в базу", e);
				}
			}

			if (prevErrorMessage != String.Empty)
				InternalMailSend(messageSubject, messageBody);
		}

		private void InternalMailSendBy(string From, string To, string mSubject, string mBody)
		{
			try
			{
				using (MailMessage Message = new MailMessage(From, To, mSubject, mBody))
				{
					SmtpClient Client = new SmtpClient(Settings.Default.SMTPHost);
					Client.Send(Message);
				}
			}
			catch(Exception e)
			{
				_logger.Error("Ошибка при отправке письма", e);
			}
		}

		private void InternalMailSend(string mSubject, string mBody)
		{
			InternalMailSendBy(Settings.Default.FarmSystemEmail, Settings.Default.SMTPWarningList, mSubject, mBody);
		}

		private void SuccesGetBody(string mSubjPref, ref string mSubj, ref string mBody, long priceCode, long clientCode, string priceName)
		{
			if (-1 == priceCode)
			{
				mSubj = mSubjPref;
				mBody = String.Format(
					@"Файл         : {0}
Дата события : {1}", 
					Path.GetFileName(_processItem.FilePath),
					DateTime.Now);
			}
			else
			{
				mSubj = String.Format("{0} {1}", mSubjPref, priceCode);
				mBody = String.Format(
					@"Код фирмы       : {0}
Код прайса      : {1}
Название прайса : {2}
Дата события    : {3}", 
					clientCode,
					priceCode,
					priceName,
					DateTime.Now);
			}
		}

		private void GetBody(string mSubjPref, ref string Add, ref string mSubj, ref string mBody, long priceCode, long clientCode, string priceName)
		{
			if (-1 == priceCode)
			{
				mSubj = mSubjPref;
				mBody = String.Format(
@"Файл         : {0}
Дата события : {1}
Ошибка       : {2}", 
					Path.GetFileName(_processItem.FilePath),
					DateTime.Now,
					Add);
				Add = mBody;
			}
			else
			{
				mSubj = String.Format("{0} {1}", mSubjPref, priceCode);
				mBody = String.Format(
@"Код фирмы       : {0}
Код прайса      : {1}
Название прайса : {2}
Дата события    : {3}
Ошибка          : {4}", 
					clientCode,
					priceCode,
					priceName,
					DateTime.Now,
					Add);
			}
		}

		private void ErrodLog(BasePriceParser p, Exception ex)
		{
			string messageBody = "", messageSubject = "", Addition;
			if (ex is FormalizeException)
				currentErrorMessage = ex.Message;
			else
				currentErrorMessage = ex.ToString();
			Addition = currentErrorMessage;
			_logger.InfoFormat("Error Addition : {0}", Addition);

			//Если предыдущее сообщение не отличается от текущего, то не логируем его
			if (prevErrorMessage != currentErrorMessage)
			{
				//Формирование заголовков письма и 
				if (null != p)
				{
					GetBody("Ошибка формализации", ref Addition, ref messageSubject, ref messageBody, p.priceCode, p.firmCode, String.Format("{0} ({1})", p.firmShortName, p.priceName) );
				}
				else
					if (ex is FormalizeException)
				{
					GetBody("Ошибка формализации", ref Addition, ref messageSubject, ref messageBody, ((FormalizeException)ex).priceCode, ((FormalizeException)ex).clientCode, ((FormalizeException)ex).FullName );
				}
				else
				{
					GetBody("Ошибка формализации", ref Addition, ref messageSubject, ref messageBody, Convert.ToInt64(_processItem.PriceCode), -1, null);
				}

				//Пытаемся залогировать в базу
				if (null != mcLog)
				{
					try
					{
						try
						{
							if (_logConnection.State == ConnectionState.Open)
								_logConnection.Close();
						}
						catch (Exception onLogCloseException)
						{
							_logger.Error("Ошибка при закрытии лог-соединения", onLogCloseException);
						}

						_logConnection.Open();
						try
						{

							mcLog.CommandText = "INSERT INTO logs.FormLogs (LogTime, Host, PriceItemId, Addition, ResultId, TotalSecs) VALUES (NOW(), ?Host, ?PriceItemId, ?Addition, ?ResultId, ?TotalSecs)";
							mcLog.Parameters.Clear();
							mcLog.Parameters.AddWithValue("?PriceItemId", _processItem.PriceItemId);
							mcLog.Parameters.AddWithValue("?Host", Environment.MachineName);
							mcLog.Parameters.AddWithValue("?Addition", Addition);
							mcLog.Parameters.AddWithValue("?ResultId", FormResults.Error);
							mcLog.Parameters.AddWithValue("?TotalSecs", formSecs);					
							mcLog.ExecuteNonQuery();
						}
						finally
						{
							_logConnection.Close();
						}
					}
					catch(Exception e)
					{
						_logger.Error("Ошибка логирования в базу", e);
					}
				}

				//Пытаемся отправить письмо по почте
				InternalMailSend(messageSubject, messageBody);
			}

		}

		private void WarningLog(FormalizeException e, string Addition)
		{
			string messageBody = "", messageSubject = "";
			currentErrorMessage = Addition;
			_logger.InfoFormat("Warning Addition : {0}", Addition);

			if (prevErrorMessage != currentErrorMessage)
			{
				//Формирование заголовков письма и 
				GetBody("Предупреждение", ref Addition, ref messageSubject, ref messageBody, e.priceCode, e.clientCode, e.FullName);

				if (e is RollbackFormalizeException)
				{
					RollbackFormalizeException re = (RollbackFormalizeException)e;
					messageBody = String.Format(
						@"{0}

Формализованно : {1}
Неформализованно : {2}
Нулевых : {3}
Запрещенных : {4}", 
						messageBody, 
						re.FormCount,
						re.UnformCount,
						re.ZeroCount,
						re.ForbCount);
				}

				//Пытаемся залогировать в базу
				if (null != mcLog)
				{
					try
					{
						try
						{
							if (_logConnection.State == System.Data.ConnectionState.Open)
								_logConnection.Close();
						}
						catch (Exception onLogCloseException)
						{
							_logger.Error("Ошибка при закрытии лог-соединения", onLogCloseException);
						}

						_logConnection.Open();
						try
						{
							if (-1 == e.priceCode)
							{
								mcLog.CommandText = "INSERT INTO logs.FormLogs (LogTime, Host, PriceItemId, Addition, ResultId, TotalSecs) VALUES (NOW(), ?Host, ?PriceItemId, ?Addition, ?ResultId, ?TotalSecs)";
								mcLog.Parameters.Clear();
							}
							else
							{
								mcLog.CommandText = "INSERT INTO logs.FormLogs (LogTime, Host, PriceItemId, Addition,Form, Unform, Zero, Forb, ResultId, TotalSecs) VALUES (NOW(), ?Host, ?PriceItemId, ?Addition, ?Form, ?Unform, ?Zero, ?Forb, ?ResultId, ?TotalSecs)";
								mcLog.Parameters.Clear();
								if (e is RollbackFormalizeException)
								{
									mcLog.Parameters.AddWithValue("?Form", ((RollbackFormalizeException)e).FormCount);
									mcLog.Parameters.AddWithValue("?Unform", ((RollbackFormalizeException)e).UnformCount);
									mcLog.Parameters.AddWithValue("?Zero", ((RollbackFormalizeException)e).ZeroCount);
									mcLog.Parameters.AddWithValue("?Forb", ((RollbackFormalizeException)e).ForbCount);
								}
								else
								{
									mcLog.Parameters.AddWithValue("?Form", DBNull.Value);
									mcLog.Parameters.AddWithValue("?Unform", DBNull.Value);
									mcLog.Parameters.AddWithValue("?Zero", DBNull.Value);
									mcLog.Parameters.AddWithValue("?Forb", DBNull.Value);
								}
							}
							mcLog.Parameters.AddWithValue("?Host", Environment.MachineName);
							mcLog.Parameters.AddWithValue("?PriceItemId", _processItem.PriceItemId);
							mcLog.Parameters.AddWithValue("?Addition", Addition);
							mcLog.Parameters.AddWithValue("?ResultId", FormResults.Error);
							mcLog.Parameters.AddWithValue("?TotalSecs", formSecs);					
							mcLog.ExecuteNonQuery();
						}
						finally
						{
							_logConnection.Close();
						}
					}
					catch(Exception ex)
					{
						_logger.Error("Ошибка логирования в базу", ex);
					}
				}

				//Пытаемся отправить письмо по почте
				InternalMailSend(messageSubject, messageBody);
			}
		}

		/// <summary>
		/// Процедура формализации
		/// </summary>
		public void ThreadWork()
		{
			_processState = PriceProcessState.Begin;
			string _allWorkTimeString = String.Empty;
			try
			{
				//имя файла для копирования в директорию Base выглядит как: <PriceItemID> + <оригинальное расширение файла>
				string outPriceFileName = FileHelper.NormalizeDir(Settings.Default.BasePath) + _processItem.PriceItemId + Path.GetExtension(_processItem.FilePath);
				//Используем идентификатор нитки в качестве названия временной папки
				TempPath = Path.GetTempPath() + TID + "\\";
				//изменяем имя файла, что оно было без недопустимых символов ('_')
				TempFileName = TempPath + _processItem.PriceItemId + Path.GetExtension(_processItem.FilePath);
				_logger.DebugFormat("Запущена нитка на обработку файла : {0}", _processItem.FilePath);

				_processState = PriceProcessState.GetConnection;
				myconn = new MySqlConnection(Literals.ConnectionString());
				_processState = PriceProcessState.GetLogConnection;
				_logConnection = new MySqlConnection(Literals.ConnectionString());
				try
				{
					_processState = PriceProcessState.CreateTempDirectory;
					//Создаем команду для логирования
					mcLog = new MySqlCommand();
					mcLog.Connection = _logConnection;

					//Создаем директорию для временного файла и копируем туда файл
					if (!Directory.Exists(TempPath))
						Directory.CreateDirectory(TempPath);
					else
					{
						//удаляем предыдущие файлы из директории, если они не были удалены
						var _tempFiles = Directory.GetFiles(TempPath);
						foreach (var _tempDeleteFile in _tempFiles)
							FileHelper.FileDelete(_tempDeleteFile);
					}

					_processState = PriceProcessState.CheckConnection;
					CheckConnection(myconn);

					try
					{
						try
						{
							_processState = PriceProcessState.CallValidate;
							WorkPrice = PricesValidator.Validate(myconn, _processItem.FilePath, TempFileName, _processItem);
						}
						finally
						{
							if (myconn.State == ConnectionState.Open)
								try
								{
									myconn.Close();
								}
								catch(Exception onWorkCloseConnection)
								{
									_logger.Error("Ошибка при закрытии рабочего соединения", onWorkCloseConnection);
								}
						}

						WorkPrice.downloaded = _processItem.Downloaded;

						_processState = PriceProcessState.CallFormalize;
						WorkPrice.Formalize();

						FormalizeOK = true;
					}
					finally
					{
						var tsFormalize = DateTime.UtcNow.Subtract(tmFormalize);
						formSecs = Convert.ToInt64(tsFormalize.TotalSeconds);
						_allWorkTimeString = tsFormalize.ToString();
					}

					_processState = PriceProcessState.FinalCopy;
					try
					{
						if (FormalizeOK)
						{
							//Если файл не скопируется, то из Inbound он не удалиться и будет попытка формализации еще раз
							File.Copy(TempFileName, outPriceFileName, true);
							var ft = DateTime.UtcNow;
							File.SetCreationTimeUtc(outPriceFileName, ft);
							File.SetLastWriteTimeUtc(outPriceFileName, ft);
							File.SetLastAccessTimeUtc(outPriceFileName, ft);
						}
					}
					catch(Exception e)
					{
						throw new FormalizeException(
							String.Format(Settings.Default.FileCopyError, TempFileName, Settings.Default.BasePath, e), 
							WorkPrice.firmCode, 
							WorkPrice.priceCode, 
							WorkPrice.firmShortName, 
							WorkPrice.priceName);
					}

					SuccesLog(WorkPrice);
				}
				catch(WarningFormalizeException e)
				{
					//Если получили человеческую ошибку, то говорим, что формализация завершилась удачно
					try
					{
						//Если файл не скопируется, то из Inbound он не удалиться и будет попытка формализации еще раз
						if (File.Exists(TempFileName))
							File.Copy(TempFileName, outPriceFileName, true);
						else
							//Копируем оригинальный файл в случае неизвестного файла
							File.Copy(_processItem.FilePath, outPriceFileName, true);
						var ft = DateTime.UtcNow;
						File.SetCreationTimeUtc(outPriceFileName, ft);
						File.SetLastWriteTimeUtc(outPriceFileName, ft);
						File.SetLastAccessTimeUtc(outPriceFileName, ft);
						WarningLog(e, e.Message);
						FormalizeOK = true;
					}
					catch(Exception ex)
					{
						FormalizeOK = false;
						ErrodLog(WorkPrice, 
							new FormalizeException(
								String.Format(Settings.Default.FileCopyError, TempFileName, Settings.Default.BasePath, ex), 
								e.clientCode, 
								e.priceCode, 
								e.clientName, 
								e.priceName));
					}
				}
				catch(FormalizeException e)
				{
					FormalizeOK = false;
					ErrodLog(WorkPrice, e);
				}
				catch(Exception e)
				{
					FormalizeOK = false;
					if ( !(e is ThreadAbortException) )
						ErrodLog(WorkPrice, e);
					else
						ErrodLog(WorkPrice, new Exception(Settings.Default.ThreadAbortError));
				}
				finally
				{
					_processState = PriceProcessState.CloseConnection;
					try
					{
						_logConnection.Close();
					}
					catch (Exception onLogCloseException)
					{
						_logger.Error("Ошибка при закрытии соединения", onLogCloseException);
					}
				}

			}
			catch(Exception e)
			{
				if (!(e is ThreadAbortException))
				{
					_logger.Error("Необработанная ошибка в нитке", e);
					InternalMailSendBy(Settings.Default.FarmSystemEmail, Settings.Default.ServiceMail, "ThreadWork Error", e.ToString());
				}
				else
					_logger.Error("Ошибка ThreadAbortException", e);
			}
			finally
			{
				_processState = PriceProcessState.FinalizeThread;
				Thread.Sleep(10);
				GC.Collect();
				Thread.Sleep(100);
				try
				{
					if (File.Exists(TempFileName))
						FileHelper.FileDelete(TempFileName);
				}
				catch(Exception e)
				{
					_logger.Error("Ошибка при удалении", e);
				}
				_logger.InfoFormat("Нитка завершила работу с прайсом {0}: {1}.", _processItem.FilePath, _allWorkTimeString);
				FormalizeEnd = true;
			}
		}

		/// <summary>
		/// Проверка connection на попытку выборки из clientsdata. Если выборка не будет успешной, то генерируем ошибку.
		/// </summary>
		/// <param name="myconn"></param>
		private void CheckConnection(MySqlConnection myconn)
		{
			if (myconn.State != ConnectionState.Open)
				myconn.Open();
			try
			{
				var dsNowTime = MySqlHelper.ExecuteDataset(myconn, "select now()");
				if (!((dsNowTime.Tables.Count == 1) && (dsNowTime.Tables[0].Rows.Count == 1)))
				{
					//Попытка получить время создания connection
					DateTime? creationTime = null;
					var driverField = myconn.GetType().GetField("driver", BindingFlags.Instance | BindingFlags.NonPublic);
					var driverInternal = driverField.GetValue(myconn);
					if (driverInternal != null)
					{
						var creationTimeField = driverInternal.GetType().GetField("creationTime", BindingFlags.Instance | BindingFlags.NonPublic);
						creationTime = (DateTime?)creationTimeField.GetValue(driverInternal);
					}

					//Пытаемся получить InnoDBStatus
					bool InnoDBByConnection = false;
					string InnoDBStatus = String.Empty;
					var dsStatus = MySqlHelper.ExecuteDataset(myconn, "show engine innodb status");
					if ((dsStatus.Tables.Count == 1) && (dsStatus.Tables[0].Rows.Count == 1) && (dsStatus.Tables[0].Columns.Contains("Status")))
					{
						InnoDBStatus = dsStatus.Tables[0].Rows[0]["Status"].ToString();
						InnoDBByConnection = true;
					}
					if (!InnoDBByConnection)
					{
						var drInnoDBStatus = MySqlHelper.ExecuteDataRow(Literals.ConnectionString(), "show engine innodb status");
						if ((drInnoDBStatus != null) && (drInnoDBStatus.Table.Columns.Contains("Status")))
							InnoDBStatus = drInnoDBStatus["Status"].ToString();
					}

					string techInfo = String.Format(@"
ServerThreadId           = {0}
CreationTime             = {1}
InnoDBStatusByConnection = {2}
InnoDB Status            =
{3}",
										myconn.ServerThread,
										creationTime,
										InnoDBByConnection,
										InnoDBStatus);

					_logger.InfoFormat("При проверке соединения получили 0 записей : {0}", techInfo);

					if (!LetterAboutConnectionSended)
						try
						{
							using (var Message = new MailMessage(
								Settings.Default.ServiceMail,
								Settings.Default.ServiceMail,
								"!!! Необходимо перезапустить PriceProcessor",
								String.Format(@"
Необходимо перезапустить PriceProcessor, т.к. в нитке формализации был получен connection, который не возвращает записей при выполнении команд.
Техническая информация:
{0}",
									techInfo)))
							{
								var Client = new SmtpClient(Settings.Default.SMTPHost);
								Client.Send(Message);
							}
							LetterAboutConnectionSended = true;
						}
						catch (Exception onSend)
						{
							_logger.Error("Ошибка при отправке письма с techInfo", onSend);
						}

					throw new Exception("При попытке выборки из clientsdata не получили записей. Перезапустите PriceProcessor.");
				}

			}
			finally
			{
				myconn.Close();
			}
		}
	}
}
