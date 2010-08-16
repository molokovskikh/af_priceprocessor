using System;
using System.IO;
using System.Threading;
using Inforoom.PriceProcessor.Formalizer.New;
using Inforoom.PriceProcessor.Helpers;
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

	public interface IPriceFormalizer
	{
		void Formalize();

		bool Downloaded { get; set; }
		string InputFileName { get; set; }
		int formCount { get; }
		int unformCount { get; }
		int zeroCount { get; }
		int forbCount { get; }
		int maxLockCount { get; }
		long priceCode { get; }
		long firmCode { get; }
		string firmShortName { get; }
		string priceName { get; }
	}

	public class PriceProcessThread
	{
		//��������� ������� ��� �����
		private string _tempPath;
		//��� �����, ������� ������� �������� ���������
		private string _tempFileName;
		//������ �� �������������� �������
		private readonly PriceProcessItem _processItem;

		private readonly Thread _thread;
		//����� ���������� ������� ����� 
		private DateTime? _abortingTime;

		//����� ������ ������������
		private readonly DateTime _startedAt;

		private readonly PriceProcessLogger _log;
		private readonly ILog _logger = LogManager.GetLogger(typeof(PriceProcessThread));

		private IPriceFormalizer _workPrice;

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

		public PriceProcessState ProcessState { get; set; }

		/// <summary>
		/// ��������� ������������
		/// </summary>
		public void ThreadWork()
		{
			ProcessState = PriceProcessState.Begin;
			var allWorkTimeString = String.Empty;
			string outPriceFileName = null;
			using(var cleaner = new FileCleaner())
			try
			{
				//��� ����� ��� ����������� � ���������� Base �������� ���: <PriceItemID> + <������������ ���������� �����>
				outPriceFileName = FileHelper.NormalizeDir(Settings.Default.BasePath) + _processItem.PriceItemId + Path.GetExtension(_processItem.FilePath);
				//���������� ������������� ����� � �������� �������� ��������� �����
				_tempPath = Path.GetTempPath() + TID + "\\";
				//�������� ��� �����, ��� ��� ���� ��� ������������ �������� ('_')
				_tempFileName = _tempPath + _processItem.PriceItemId + Path.GetExtension(_processItem.FilePath);
				cleaner.Watch(_tempFileName);
				_logger.DebugFormat("�������� ����� �� ��������� ����� : {0}", _processItem.FilePath);

				ProcessState = PriceProcessState.CreateTempDirectory;

				//������� ���������� ��� ���������� ����� � �������� ���� ����
				if (Directory.Exists(_tempPath))
					FileHelper.DeleteDir(_tempPath);

				Directory.CreateDirectory(_tempPath);

				try
				{
					ProcessState = PriceProcessState.CallValidate;
					_workPrice = PricesValidator.Validate(_processItem.FilePath, _tempFileName, (uint)_processItem.PriceItemId);

					_workPrice.Downloaded = _processItem.Downloaded;
					_workPrice.InputFileName = _processItem.FilePath;

					ProcessState = PriceProcessState.CallFormalize;
					_workPrice.Formalize();

					FormalizeOK = true;
					_log.SuccesLog(_workPrice);
				}
				catch (WarningFormalizeException e)
				{
					_log.WarningLog(e, e.Message);
					FormalizeOK = true;
				}
				finally
				{
					var tsFormalize = DateTime.UtcNow.Subtract(_startedAt);
					_log.FormSecs = Convert.ToInt64(tsFormalize.TotalSeconds);
					allWorkTimeString = tsFormalize.ToString();
				}

				ProcessState = PriceProcessState.FinalCopy;
				//���� ���� �� �����������, �� �� Inbound �� �� ��������� � ����� ������� ������������ ��� ���
				if (File.Exists(_tempFileName))
					File.Copy(_tempFileName, outPriceFileName, true);
				else
					File.Copy(_processItem.FilePath, outPriceFileName, true);
				var ft = DateTime.UtcNow;
				File.SetCreationTimeUtc(outPriceFileName, ft);
				File.SetLastWriteTimeUtc(outPriceFileName, ft);
				File.SetLastAccessTimeUtc(outPriceFileName, ft);
			}
			catch(Exception e)
			{
				if (e is ThreadAbortException)
					_log.ErrodLog(_workPrice, new Exception(Settings.Default.ThreadAbortError));
				else
				{
					_log.ErrodLog(_workPrice, e);
					Mailer.SendFromServiceToService("������ ��� ������������ ����� �����", e.ToString());
				}
			}
			finally
			{
				ProcessState = PriceProcessState.FinalizeThread;
				_logger.InfoFormat("����� ��������� ������ � ������� {0}: {1}.", _processItem.FilePath, allWorkTimeString);
				FormalizeEnd = true;
			}
		}
	}
}
