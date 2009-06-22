using System;
using System.IO;
using System.Threading;
using log4net;
using MySql.Data.MySqlClient;
using Inforoom.PriceProcessor.Properties;
using Inforoom.PriceProcessor;
using System.Data;
using System.Reflection;
using FileHelper=Inforoom.Common.FileHelper;

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

	public class PriceProcessThread
	{
		//��������� ������� ��� �����
		private string _tempPath;
		//��� �����, ������� ������� �������� ���������
		private string _tempFileName;
		//������ �� �������������� �������
		private readonly PriceProcessItem _processItem;
		//���������� � �����
		private MySqlConnection _connection;

		private readonly Thread _thread;
		//����� ���������� ������� ����� 
		private DateTime? _abortingTime;

		//����� ������ ������������
		private readonly DateTime _startedAt;

		private readonly PriceProcessLogger _log;
		private readonly ILog _logger = LogManager.GetLogger(typeof(PriceProcessThread));

		private BasePriceParser _workPrice;

		private PriceProcessState _processState = PriceProcessState.None;

		public PriceProcessThread(PriceProcessItem item, string prevErrorMessage)
		{
			_startedAt = DateTime.UtcNow;
			_log = new PriceProcessLogger(prevErrorMessage, item);
			_processItem = item;
			_thread = new Thread(ThreadWork);
			_thread.Name = String.Format("PPT{0}", _thread.ManagedThreadId);
			_thread.Start();
		}

		public bool FormalizeOK { get; private set; }

		/// <summary>
		/// ������� � ���, ��� ������������ ���������. ��������� � ��� ������, ���� ����� �� ������� ����� ����� �������,
		/// ����� ���� �������� �� ThreadState
		/// </summary>
		public bool FormalizeEnd { get; private set; }

		/// <summary>
		/// ����� ������ ������������
		/// </summary>
		public DateTime StartDate
		{
			get
			{
				return _startedAt;
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
					(((_thread.ThreadState & ThreadState.AbortRequested) > 0) || ((_thread.ThreadState & ThreadState.Aborted) > 0)) 
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
				return _log.CurrentErrorMessage;
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

		/// <summary>
		/// ��������� ������������
		/// </summary>
		public void ThreadWork()
		{
			_processState = PriceProcessState.Begin;
			var allWorkTimeString = String.Empty;
			try
			{
				//��� ����� ��� ����������� � ���������� Base �������� ���: <PriceItemID> + <������������ ���������� �����>
				var outPriceFileName = FileHelper.NormalizeDir(Settings.Default.BasePath) + _processItem.PriceItemId + Path.GetExtension(_processItem.FilePath);
				//���������� ������������� ����� � �������� �������� ��������� �����
				_tempPath = Path.GetTempPath() + TID + "\\";
				//�������� ��� �����, ��� ��� ���� ��� ������������ �������� ('_')
				_tempFileName = _tempPath + _processItem.PriceItemId + Path.GetExtension(_processItem.FilePath);
				_logger.DebugFormat("�������� ����� �� ��������� ����� : {0}", _processItem.FilePath);

				_processState = PriceProcessState.GetConnection;
				_connection = new MySqlConnection(Literals.ConnectionString());
				_processState = PriceProcessState.GetLogConnection;
				try
				{
					_processState = PriceProcessState.CreateTempDirectory;

					//������� ���������� ��� ���������� ����� � �������� ���� ����
					if (!Directory.Exists(_tempPath))
						Directory.CreateDirectory(_tempPath);
					else
					{
						//������� ���������� ����� �� ����������, ���� ��� �� ���� �������
						var tempFiles = Directory.GetFiles(_tempPath);
						foreach (var tempDeleteFile in tempFiles)
							FileHelper.FileDelete(tempDeleteFile);
					}

					_processState = PriceProcessState.CheckConnection;
					CheckConnection(_connection);

					try
					{
						try
						{
							_processState = PriceProcessState.CallValidate;
							_workPrice = PricesValidator.Validate(_connection, _processItem.FilePath, _tempFileName, _processItem);
						}
						finally
						{
							if (_connection.State == ConnectionState.Open)
								try
								{
									_connection.Close();
								}
								catch(Exception onWorkCloseConnection)
								{
									_logger.Error("������ ��� �������� �������� ����������", onWorkCloseConnection);
								}
						}

						_workPrice.downloaded = _processItem.Downloaded;

						_processState = PriceProcessState.CallFormalize;
						_workPrice.Formalize();

						FormalizeOK = true;
					}
					finally
					{
						var tsFormalize = DateTime.UtcNow.Subtract(_startedAt);
						_log.FormSecs = Convert.ToInt64(tsFormalize.TotalSeconds);
						allWorkTimeString = tsFormalize.ToString();
					}

					_processState = PriceProcessState.FinalCopy;
					try
					{
						if (FormalizeOK)
						{
							//���� ���� �� �����������, �� �� Inbound �� �� ��������� � ����� ������� ������������ ��� ���
							File.Copy(_tempFileName, outPriceFileName, true);
							var ft = DateTime.UtcNow;
							File.SetCreationTimeUtc(outPriceFileName, ft);
							File.SetLastWriteTimeUtc(outPriceFileName, ft);
							File.SetLastAccessTimeUtc(outPriceFileName, ft);
						}
					}
					catch(Exception e)
					{
						throw new FormalizeException(
							String.Format(Settings.Default.FileCopyError, _tempFileName, Settings.Default.BasePath, e), 
							_workPrice.firmCode, 
							_workPrice.priceCode, 
							_workPrice.firmShortName, 
							_workPrice.priceName);
					}

					_log.SuccesLog(_workPrice);
				}
				catch(WarningFormalizeException e)
				{
					//���� �������� ������������ ������, �� �������, ��� ������������ ����������� ������
					try
					{
						//���� ���� �� �����������, �� �� Inbound �� �� ��������� � ����� ������� ������������ ��� ���
						if (File.Exists(_tempFileName))
							File.Copy(_tempFileName, outPriceFileName, true);
						else
							//�������� ������������ ���� � ������ ������������ �����
							File.Copy(_processItem.FilePath, outPriceFileName, true);
						var ft = DateTime.UtcNow;
						File.SetCreationTimeUtc(outPriceFileName, ft);
						File.SetLastWriteTimeUtc(outPriceFileName, ft);
						File.SetLastAccessTimeUtc(outPriceFileName, ft);
						_log.WarningLog(e, e.Message);
						FormalizeOK = true;
					}
					catch(Exception ex)
					{
						FormalizeOK = false;
						_log.ErrodLog(_workPrice,
						              new FormalizeException(
						              	String.Format(Settings.Default.FileCopyError, _tempFileName, Settings.Default.BasePath, ex),
						              	e.clientCode,
						              	e.priceCode,
						              	e.clientName,
						              	e.priceName));
					}
				}
				catch(FormalizeException e)
				{
					FormalizeOK = false;
					_log.ErrodLog(_workPrice, e);
				}
				catch(Exception e)
				{
					FormalizeOK = false;
					if ( !(e is ThreadAbortException) )
						_log.ErrodLog(_workPrice, e);
					else
						_log.ErrodLog(_workPrice, new Exception(Settings.Default.ThreadAbortError));
				}
				finally
				{
					_processState = PriceProcessState.CloseConnection;
				}
			}
			catch(Exception e)
			{
				if (!(e is ThreadAbortException))
				{
					_logger.Error("�������������� ������ � �����", e);
					Mailer.SendFromFarmToService("ThreadWork Error", e.ToString());
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
					if (File.Exists(_tempFileName))
						FileHelper.FileDelete(_tempFileName);
				}
				catch(Exception e)
				{
					_logger.Error("������ ��� ��������", e);
				}
				_logger.InfoFormat("����� ��������� ������ � ������� {0}: {1}.", _processItem.FilePath, allWorkTimeString);
				FormalizeEnd = true;
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
				var dsNowTime = MySqlHelper.ExecuteDataset(myconn, "select now()");
				if (!((dsNowTime.Tables.Count == 1) && (dsNowTime.Tables[0].Rows.Count == 1)))
				{
					//������� �������� ����� �������� connection
					DateTime? creationTime = null;
					var driverField = myconn.GetType().GetField("driver", BindingFlags.Instance | BindingFlags.NonPublic);
					var driverInternal = driverField.GetValue(myconn);
					if (driverInternal != null)
					{
						var creationTimeField = driverInternal.GetType().GetField("creationTime", BindingFlags.Instance | BindingFlags.NonPublic);
						creationTime = (DateTime?)creationTimeField.GetValue(driverInternal);
					}

					//�������� �������� InnoDBStatus
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

					_logger.InfoFormat("��� �������� ���������� �������� 0 ������� : {0}", techInfo);
					_log.SendMySqlFailMessage(techInfo);

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
