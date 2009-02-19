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
		//���� �����������, �� ������ � ErrorList � ��������� � connection ����������
		private static bool LetterAboutConnectionSended = false;

		//��������� ������� ��� �����
		private string TempPath;
		//��� �����, ������� ������� �������� ���������
		private string TempFileName;

		/// <summary>
		/// ������ �� �������������� �������
		/// </summary>
		private PriceProcessItem _processItem;

		//���������� � �����
		private MySqlConnection myconn;
		private MySqlConnection _logConnection;
		private MySqlCommand mcLog = null;

		//���������� �����
		private Thread _thread;
		//����� ���������� ������� ����� 
		private DateTime? _abortingTime = null;

		//����� ������ ������������
		private DateTime tmFormalize;
		//����� ������������ � ��������
		private System.Int64 formSecs;

		private bool _formalizeEnd = false;

		//��������� ������������: ������, �����
		private bool formalizeOK = false;

		//���������� ��������� �� ������
		private string prevErrorMessage = String.Empty;
		//������� ��������� �� ������
		private string currentErrorMessage = String.Empty; 

		private BasePriceParser WorkPrice = null;

		private readonly log4net.ILog _logger = log4net.LogManager.GetLogger(typeof(PriceProcessThread));

		private PriceProcessState _processState = PriceProcessState.None;

		public PriceProcessThread(PriceProcessItem item, string PrevErrorMessage)
		{
			this.tmFormalize = DateTime.UtcNow;
			this.prevErrorMessage = PrevErrorMessage;
			this._processItem = item;
			this._thread = new Thread(new ThreadStart(ThreadWork));
			this._thread.Name = String.Format("PPT{0}", this._thread.ManagedThreadId);
			this._thread.Start();
		}

		public bool FormalizeOK
		{
			get
			{
				return formalizeOK;
			}
		}

		/// <summary>
		/// ������� � ���, ��� ������������ ���������. ��������� � ��� ������, ���� ����� �� ������� ����� ����� �������,
		/// ����� ���� �������� �� ThreadState
		/// </summary>
		public bool FormalizeEnd
		{
			get
			{
				return _formalizeEnd;
			}
		}

		/// <summary>
		/// ����� ������ ������������
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
		/// ������������� ������� ����� � ���������� ����� ��������, ����� �������� �� ��������
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
		/// �������� ����� Interrupt � �����, ���� ��� ��������� � ��������� AbortRequested � WaitSleepJoin
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
		/// ������ �� �������������� �������
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

		private MySqlConnection getConnection()
		{
			return new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
		}

		private void SuccesLog(BasePriceParser p)
		{
			string messageBody = "", messageSubject = "";
			//������������ ���������� ������ � 
			if (null == p)
			{
				SuccesGetBody("����� ������ ������������", ref messageSubject, ref messageBody, -1, -1, null);
			}
			else
			{
				SuccesGetBody("����� ������ ������������", ref messageSubject, ref messageBody, p.priceCode, p.firmCode, String.Format("{0} ({1})", p.firmShortName, p.priceName) );
			}


			if (null != mcLog)
			{
				try
				{
					try
					{
						if (_logConnection.State == System.Data.ConnectionState.Open)
							_logConnection.Close();
					}
					catch(Exception onLogCloseException)
					{
						_logger.Error("������ ��� �������� ���-����������", onLogCloseException);
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
					_logger.Error("������ ����������� � ����", e);
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
				_logger.Error("������ ��� �������� ������", e);
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
					@"����         : {0}
���� ������� : {1}", 
					Path.GetFileName(_processItem.FilePath),
					DateTime.Now);
			}
			else
			{
				mSubj = String.Format("{0} {1}", mSubjPref, priceCode);
				mBody = String.Format(
					@"��� �����       : {0}
��� ������      : {1}
�������� ������ : {2}
���� �������    : {3}", 
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
@"����         : {0}
���� ������� : {1}
������       : {2}", 
					Path.GetFileName(_processItem.FilePath),
					DateTime.Now,
					Add);
				Add = mBody;
			}
			else
			{
				mSubj = String.Format("{0} {1}", mSubjPref, priceCode);
				mBody = String.Format(
@"��� �����       : {0}
��� ������      : {1}
�������� ������ : {2}
���� �������    : {3}
������          : {4}", 
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

			//���� ���������� ��������� �� ���������� �� ��������, �� �� �������� ���
			if (prevErrorMessage != currentErrorMessage)
			{
				//������������ ���������� ������ � 
				if (null != p)
				{
					GetBody("������ ������������", ref Addition, ref messageSubject, ref messageBody, p.priceCode, p.firmCode, String.Format("{0} ({1})", p.firmShortName, p.priceName) );
				}
				else
					if (ex is FormalizeException)
				{
					GetBody("������ ������������", ref Addition, ref messageSubject, ref messageBody, ((FormalizeException)ex).priceCode, ((FormalizeException)ex).clientCode, ((FormalizeException)ex).FullName );
				}
				else
				{
					GetBody("������ ������������", ref Addition, ref messageSubject, ref messageBody, Convert.ToInt64(_processItem.PriceCode), -1, null);
				}

				//�������� ������������ � ����
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
							_logger.Error("������ ��� �������� ���-����������", onLogCloseException);
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
						_logger.Error("������ ����������� � ����", e);
					}
				}

				//�������� ��������� ������ �� �����
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
				//������������ ���������� ������ � 
				GetBody("��������������", ref Addition, ref messageSubject, ref messageBody, e.priceCode, e.clientCode, e.FullName);

				if (e is RollbackFormalizeException)
				{
					RollbackFormalizeException re = (RollbackFormalizeException)e;
					messageBody = String.Format(
						@"{0}

�������������� : {1}
���������������� : {2}
������� : {3}
����������� : {4}", 
						messageBody, 
						re.FormCount,
						re.UnformCount,
						re.ZeroCount,
						re.ForbCount);
				}

				//�������� ������������ � ����
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
							_logger.Error("������ ��� �������� ���-����������", onLogCloseException);
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
						_logger.Error("������ ����������� � ����", ex);
					}
				}

				//�������� ��������� ������ �� �����
				InternalMailSend(messageSubject, messageBody);
			}
		}

		/// <summary>
		/// ��������� ������������
		/// </summary>
		public void ThreadWork()
		{
			_processState = PriceProcessState.Begin;
			string _allWorkTimeString = String.Empty;
			try
			{
				//��� ����� ��� ����������� � ���������� Base �������� ���: <PriceItemID> + <������������ ���������� �����>
				string outPriceFileName = FileHelper.NormalizeDir(Settings.Default.BasePath) + _processItem.PriceItemId.ToString() + Path.GetExtension(_processItem.FilePath);
				//���������� ������������� ����� � �������� �������� ��������� �����
				TempPath = Path.GetTempPath() + TID + "\\";
				//�������� ��� �����, ��� ��� ���� ��� ������������ �������� ('_')
				TempFileName = TempPath + _processItem.PriceItemId.ToString() + Path.GetExtension(_processItem.FilePath);
				_logger.DebugFormat("�������� ����� �� ��������� ����� : {0}", _processItem.FilePath);

				_processState = PriceProcessState.GetConnection;
				myconn = getConnection();
				_processState = PriceProcessState.GetLogConnection;
				_logConnection = getConnection();
				try
				{
					_processState = PriceProcessState.CreateTempDirectory;
					//������� ������� ��� �����������
					mcLog = new MySqlCommand();
					mcLog.Connection = _logConnection;

					//������� ���������� ��� ���������� ����� � �������� ���� ����
					if (!Directory.Exists(TempPath))
						Directory.CreateDirectory(TempPath);
					else
					{
						//������� ���������� ����� �� ����������, ���� ��� �� ���� �������
						string[] _tempFiles = Directory.GetFiles(TempPath);
						foreach (string _tempDeleteFile in _tempFiles)
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
							if (myconn.State == System.Data.ConnectionState.Open)
								try
								{
									myconn.Close();
								}
								catch(Exception onWorkCloseConnection)
								{
									_logger.Error("������ ��� �������� �������� ����������", onWorkCloseConnection);
								}
						}

						WorkPrice.downloaded = _processItem.Downloaded;

						_processState = PriceProcessState.CallFormalize;
						WorkPrice.Formalize();

						formalizeOK = true;
					}
					finally
					{
						TimeSpan tsFormalize = DateTime.UtcNow.Subtract(tmFormalize);
						formSecs = Convert.ToInt64(tsFormalize.TotalSeconds);
						_allWorkTimeString = tsFormalize.ToString();
					}

					_processState = PriceProcessState.FinalCopy;
					try
					{
						if (formalizeOK)
						{
							//���� ���� �� �����������, �� �� Inbound �� �� ��������� � ����� ������� ������������ ��� ���
							File.Copy(TempFileName, outPriceFileName, true);
							DateTime ft = DateTime.UtcNow;
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
					//���� �������� ������������ ������, �� �������, ��� ������������ ����������� ������
					try
					{
						//���� ���� �� �����������, �� �� Inbound �� �� ��������� � ����� ������� ������������ ��� ���
						if (File.Exists(TempFileName))
							File.Copy(TempFileName, outPriceFileName, true);
						else
							//�������� ������������ ���� � ������ ������������ �����
							File.Copy(_processItem.FilePath, outPriceFileName, true);
						DateTime ft = DateTime.UtcNow;
						File.SetCreationTimeUtc(outPriceFileName, ft);
						File.SetLastWriteTimeUtc(outPriceFileName, ft);
						File.SetLastAccessTimeUtc(outPriceFileName, ft);
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
					_processState = PriceProcessState.CloseConnection;
					try
					{
						myconn.Close();
					}
					catch (Exception onCloseException)
					{
						_logger.Error("������ ��� �������� ����������", onCloseException);
					}
					try
					{
						_logConnection.Close();
					}
					catch (Exception onLogCloseException)
					{
						_logger.Error("������ ��� �������� ����������", onLogCloseException);
					}
				}

			}
			catch(Exception e)
			{
				if (!(e is System.Threading.ThreadAbortException))
				{
					_logger.Error("�������������� ������ � �����", e);
					InternalMailSendBy(Settings.Default.FarmSystemEmail, Settings.Default.ServiceMail, "ThreadWork Error", e.ToString());
				}
				else
					_logger.Error("������ ThreadAbortException", e);
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
					_logger.Error("������ ��� ��������", e);
				}
				_logger.InfoFormat("����� ��������� ������ � ������� {0}: {1}.", _processItem.FilePath, _allWorkTimeString);
				_formalizeEnd = true;
			}
		}

		/// <summary>
		/// �������� connection �� ������� ������� �� clientsdata. ���� ������� �� ����� ��������, �� ���������� ������.
		/// </summary>
		/// <param name="myconn"></param>
		private void CheckConnection(MySqlConnection myconn)
		{
			if (myconn.State != ConnectionState.Open)
				myconn.Open();
			try
			{
				DataSet dsNowTime = MySqlHelper.ExecuteDataset(myconn, "select now()");
				if (!((dsNowTime.Tables.Count == 1) && (dsNowTime.Tables[0].Rows.Count == 1)))
				{
					//������� �������� ����� �������� connection
					DateTime? creationTime = null;
					FieldInfo driverField = myconn.GetType().GetField("driver", BindingFlags.Instance | BindingFlags.NonPublic);
					object driverInternal = driverField.GetValue(myconn);
					if (driverInternal != null)
					{
						FieldInfo creationTimeField = driverInternal.GetType().GetField("creationTime", BindingFlags.Instance | BindingFlags.NonPublic);
						creationTime = (DateTime?)creationTimeField.GetValue(driverInternal);
					}

					//�������� �������� InnoDBStatus
					bool InnoDBByConnection = false;
					string InnoDBStatus = String.Empty;
					DataSet dsStatus = MySqlHelper.ExecuteDataset(myconn, "show engine innodb status");
					if ((dsStatus.Tables.Count == 1) && (dsStatus.Tables[0].Rows.Count == 1) && (dsStatus.Tables[0].Columns.Contains("Status")))
					{
						InnoDBStatus = dsStatus.Tables[0].Rows[0]["Status"].ToString();
						InnoDBByConnection = true;
					}
					if (!InnoDBByConnection)
					{
						DataRow drInnoDBStatus = MySqlHelper.ExecuteDataRow(ConfigurationManager.ConnectionStrings["DB"].ConnectionString, "show engine innodb status");
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

					_logger.InfoFormat("��� �������� ���������� �������� 0 ������� : {0}", techInfo);

					if (!LetterAboutConnectionSended)
						try
						{
							using (MailMessage Message = new MailMessage(
								Settings.Default.ServiceMail,
								Settings.Default.ServiceMail,
								"!!! ���������� ������������� PriceProcessor",
								String.Format(@"
���������� ������������� PriceProcessor, �.�. � ����� ������������ ��� ������� connection, ������� �� ���������� ������� ��� ���������� ������.
����������� ����������:
{0}",
									techInfo)))
							{
								SmtpClient Client = new SmtpClient(Settings.Default.SMTPHost);
								Client.Send(Message);
							}
							LetterAboutConnectionSended = true;
						}
						catch (Exception onSend)
						{
							_logger.Error("������ ��� �������� ������ � techInfo", onSend);
						}

					throw new Exception("��� ������� ������� �� clientsdata �� �������� �������. ������������� PriceProcessor.");
				}

			}
			finally
			{
				myconn.Close();
			}
		}
	}
}
