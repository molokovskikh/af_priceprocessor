using System;
using System.IO;
using System.Threading;
using System.Net.Mail;
using MySql.Data.MySqlClient;
using Inforoom.Logging;
using Inforoom.PriceProcessor.Properties;
using System.Configuration;


namespace Inforoom.Formalizer
{

	public class FileHashItem
	{
		public string ErrorMessage = String.Empty;
		public int ErrorCount = 0;
	}
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class PriceProcessThread
	{
		//идентификатор нити
		private static int GlobalPPTID = -1;
		private int PPTID;
		//Имя обрабатываемого файла
		private string fileName;
		//Временный каталог для файла
		private string TempPath;
		//Имя файла, который реально проходит обработку
		private string TempFileName;

		//Соединение с базой
		private MySqlConnection myconn;
		private MySqlCommand mcLog = null;

//		private int formID;

		//Событие, выполняемое в нити
		private ThreadStart ts;

		//Собственно нитка
		private Thread t;

		//Время начала формализации
		private DateTime tmFormalize;
		//Время формализации в секундах
		private System.Int64 formSecs;

		public bool FormalizeEnd = false;

		//Результат формализации: Хорошо, Плохо
		private bool formalizeOK = false;

		//предыдущее сообщение об ошибке
		private string prevErrorMessage = String.Empty;
		//текущее сообщение об ошибке
		private string currentErrorMessage = String.Empty;
 

		private BasePriceParser WorkPrice = null;

		public PriceProcessThread(string FileName, string PrevErrorMessage)
		{
			this.tmFormalize = DateTime.UtcNow;
			this.fileName = FileName;
			this.PPTID = ++GlobalPPTID;
			this.prevErrorMessage = PrevErrorMessage;
			ts = new ThreadStart(ThreadWork);
			t = new Thread(ts);
			t.Name = String.Format("PPT{0}", PPTID);
			t.Start();

		}

		public string FileName
		{
			get
			{
				return fileName;
			}
		}

		public bool FormalizeOK
		{
			get
			{
				return formalizeOK;
			}
		}
		//Показать время начала формализации
		public DateTime StartDate
		{
			get
			{
				return tmFormalize;
			}
		}

		public Thread WorkThread
		{
			get
			{
				return t;
			}
		}

		public int TID
		{
			get
			{
				return PPTID;
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
		/// Внутренее логирование
		/// </summary>
		/// <param name="Message"></param>
		private void InternalLog(string Message)
		{
			SimpleLog.Log( String.Format("TID={0}", PPTID), Message);
		}

		private void InternalLog(string format, params object[] args)
		{
			InternalLog(String.Format(format, args));
		}

		private MySqlConnection getConnection()
		{
			return new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
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
				SuccesGetBody("Прайс упешно формализован", ref messageSubject, ref messageBody, p.priceCode, p.clientCode, String.Format("{0} ({1})", p.clientShortName, p.priceName) );
			}


			if (null != mcLog)
			{
				try
				{
					try
					{
						if (myconn.State == System.Data.ConnectionState.Open)
							myconn.Close();
					}
					catch{}

					myconn.Open();
					try
					{
						mcLog.CommandText = "INSERT INTO logs.FormLogs (LogTime, Host, PriceCode, Form, Unform, Zero, Forb, ResultId, TotalSecs) VALUES (NOW(), ?Host, ?PriceCode, ?Form, ?Unform, ?Zero, ?Forb, ?ResultId, ?TotalSecs )";
						mcLog.Parameters.Clear();
						mcLog.Parameters.AddWithValue("?Host", Environment.MachineName);
						mcLog.Parameters.AddWithValue("?PriceCode", p.priceCode);
						mcLog.Parameters.AddWithValue("?Form", p.formCount);
						mcLog.Parameters.AddWithValue("?Unform", p.unformCount);
						mcLog.Parameters.AddWithValue("?Zero", p.zeroCount);
						mcLog.Parameters.AddWithValue("?Forb", p.forbCount);
						mcLog.Parameters.AddWithValue("?ResultId", (p.maxLockCount <= Settings.Default.MinRepeatTranCount) ? 2 : 3);
						mcLog.Parameters.AddWithValue("?TotalSecs", formSecs);					
						mcLog.ExecuteNonQuery();
					}
					finally
					{
						myconn.Close();
					}
				}
				catch(Exception e)
				{
					InternalLog("SuccesLog : {0}", e);
				}
			}

			if (prevErrorMessage != String.Empty)
				InternalMailSend(messageSubject, messageBody);
		}

		private void InternalMailSendBy(string From, string To, string mSubject, string mBody)
		{
			try
			{
				MailMessage Message = new MailMessage(From, To, mSubject, mBody);
				Message.BodyEncoding = System.Text.Encoding.UTF8;
				SmtpClient Client = new SmtpClient(Settings.Default.SMTPHost);
				Client.Send(Message);
			}
			catch(Exception e)
			{
				InternalLog("ErrorLog.InternalMailSendBy : {0}", e);
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
					Path.GetFileName(fileName),
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
					Path.GetFileName(fileName),
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
			InternalLog("Addition : {0}", Addition);

			//Если предыдущее сообщение не отличается от текущего, то не логируем его
			if (prevErrorMessage != currentErrorMessage)
			{
				//Формирование заголовков письма и 
				if (null != p)
				{
					GetBody("Ошибка формализации", ref Addition, ref messageSubject, ref messageBody, p.priceCode, p.clientCode, String.Format("{0} ({1})", p.clientShortName, p.priceName) );
				}
				else
					if (ex is FormalizeException)
				{
					GetBody("Ошибка формализации", ref Addition, ref messageSubject, ref messageBody, ((FormalizeException)ex).priceCode, ((FormalizeException)ex).clientCode, ((FormalizeException)ex).FullName );
				}
				else
				{
					GetBody("Ошибка формализации", ref Addition, ref messageSubject, ref messageBody, -1, -1, null);
				}

				//Пытаемся залогировать в базу
				if (null != mcLog)
				{
					try
					{
						try
						{
							if (myconn.State == System.Data.ConnectionState.Open)
								myconn.Close();
						}
						catch{}

						myconn.Open();
						try
						{

							if ((null != p) || (ex is FormalizeException))
							{
								mcLog.CommandText = "INSERT INTO logs.FormLogs (LogTime, Host, PriceCode, Addition, ResultId, TotalSecs) VALUES (NOW(), ?Host, ?PriceCode, ?Addition, ?ResultId, ?TotalSecs)";
								mcLog.Parameters.Clear();
								mcLog.Parameters.AddWithValue("?PriceCode", (null != p) ? p.priceCode : ((FormalizeException)ex).priceCode);
							}
							else
							{
								mcLog.CommandText = "INSERT INTO logs.FormLogs (LogTime, Host, Addition, ResultId, TotalSecs) VALUES (NOW(), ?Host, ?Addition, ?ResultId, ?TotalSecs)";
								mcLog.Parameters.Clear();
							}
							mcLog.Parameters.AddWithValue("?Host", Environment.MachineName);
							mcLog.Parameters.AddWithValue("?Addition", Addition);
							mcLog.Parameters.AddWithValue("?ResultId", 5);
							mcLog.Parameters.AddWithValue("?TotalSecs", formSecs);					
							mcLog.ExecuteNonQuery();
						}
						finally
						{
							myconn.Close();
						}
					}
					catch(Exception e)
					{
						InternalLog("ErrorLog : {0}", e);
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
			InternalLog("Addition : {0}", Addition);

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
							if (myconn.State == System.Data.ConnectionState.Open)
								myconn.Close();
						}
						catch{}

						myconn.Open();
						try
						{
							if (-1 == e.priceCode)
							{
								mcLog.CommandText = "INSERT INTO logs.FormLogs (LogTime, Host, Addition, ResultId, TotalSecs) VALUES (NOW(), ?Host, ?Addition, ?ResultId, ?TotalSecs)";
								mcLog.Parameters.Clear();
								mcLog.Parameters.AddWithValue("?Host", Environment.MachineName);
							}
							else
							{
								mcLog.CommandText = "INSERT INTO logs.FormLogs (LogTime, Host, PriceCode, Addition,Form, Unform, Zero, Forb, ResultId, TotalSecs) VALUES (NOW(), ?Host, ?PriceCode, ?Addition, ?Form, ?Unform, ?Zero, ?Forb, ?ResultId, ?TotalSecs)";
								mcLog.Parameters.Clear();
								mcLog.Parameters.AddWithValue("?Host", Environment.MachineName);
								mcLog.Parameters.AddWithValue("?PriceCode", e.priceCode);
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
							mcLog.Parameters.AddWithValue("?Addition", Addition);
							mcLog.Parameters.AddWithValue("?ResultId", 5);
							mcLog.Parameters.AddWithValue("?TotalSecs", formSecs);					
							mcLog.ExecuteNonQuery();
						}
						finally
						{
							myconn.Close();
						}
					}
					catch(Exception ex)
					{
						InternalLog("WarningLog : {0}", ex);
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
			string workStr = String.Empty;
			//TempPath = Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location ) + "\\" + PPTID + "\\";
			TempPath = Path.GetTempPath() + Path.GetFileNameWithoutExtension(fileName) + "\\";
			TempFileName = TempPath + Path.GetFileName(fileName);
			InternalLog("Запущена нитка на обработку файла : {0}", fileName);
			try
			{
				myconn = getConnection();
				try
				{
					//Создаем команду для логирования
					mcLog = new MySqlCommand();
					mcLog.Connection = myconn;

					//Создаем директорию для временного файла и копируем туда файл
					Directory.CreateDirectory(TempPath);

					try
					{
						try
						{
							WorkPrice = PricesValidator.Validate(myconn, fileName, TempFileName);
						}
						finally
						{
							if (myconn.State == System.Data.ConnectionState.Open)
								try
								{
									myconn.Close();
								}
								catch
								{}
						}

						WorkPrice.Formalize();

						formalizeOK = true;
					}
					finally
					{
						TimeSpan tsFormalize = DateTime.UtcNow.Subtract(tmFormalize);
						formSecs = Convert.ToInt64(tsFormalize.TotalSeconds);
						workStr = tsFormalize.ToString();
					}

					try
					{
						if (formalizeOK)
						{
							//Если файл не скопируется, то из Inbound он не удалиться и будет попытка формализации еще раз
							File.Copy(TempFileName, Settings.Default.BasePath + Path.GetFileName(fileName), true);
							DateTime ft = DateTime.UtcNow;
							File.SetCreationTimeUtc(Settings.Default.BasePath + Path.GetFileName(fileName), ft);
							File.SetLastWriteTimeUtc(Settings.Default.BasePath + Path.GetFileName(fileName), ft);
							File.SetLastAccessTimeUtc(Settings.Default.BasePath + Path.GetFileName(fileName), ft);
						}
					}
					catch(Exception e)
					{
						throw new FormalizeException(
							String.Format(Settings.Default.FileCopyError, TempFileName, Settings.Default.BasePath, e), 
							WorkPrice.clientCode, 
							WorkPrice.priceCode, 
							WorkPrice.clientShortName, 
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
							File.Copy(TempFileName, Settings.Default.BasePath + Path.GetFileName(fileName), true);
						else
							//Копируем оригинальный файл в случае неизвестного файла
							File.Copy(fileName, Settings.Default.BasePath + Path.GetFileName(fileName), true);
						DateTime ft = DateTime.UtcNow;
						File.SetCreationTimeUtc(Settings.Default.BasePath + Path.GetFileName(fileName), ft);
						File.SetLastWriteTimeUtc(Settings.Default.BasePath + Path.GetFileName(fileName), ft);
						File.SetLastAccessTimeUtc(Settings.Default.BasePath + Path.GetFileName(fileName), ft);
						WarningLog(e, e.Message);
						formalizeOK = true;
					}
					catch(Exception ex)
					{
						formalizeOK = false;
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
					formalizeOK = false;
					ErrodLog(WorkPrice, e);
				}
				catch(Exception e)
				{
					formalizeOK = false;
					if ( !(e is System.Threading.ThreadAbortException) )
						ErrodLog(WorkPrice, e);
					else
						ErrodLog(WorkPrice, new Exception(Settings.Default.ThreadAbortError));
				}
				finally
				{
					try
					{
						myconn.Close();
						myconn.Dispose();
					}
					catch
					{}
				}

			}
			catch(Exception e)
			{
				if (!(e is System.Threading.ThreadAbortException))
				{
					InternalLog(e.ToString());
					InternalMailSendBy(Settings.Default.FarmSystemEmail, "service@analit.net", "ThreadWork Error", e.ToString());
				}
			}
			finally
			{
				GC.Collect();
				try
				{
					File.Delete(TempFileName);
				}
				catch(Exception e)
				{
					InternalLog("Ошибка при удалении {0}", e);
				}
				InternalLog( "Нитка завершила работу с прайсом {0}: {1}.", fileName, workStr);
				FormalizeEnd = true;
			}
		}
	}
}
