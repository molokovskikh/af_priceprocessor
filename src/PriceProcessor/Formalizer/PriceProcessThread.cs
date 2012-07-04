using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Inforoom.PriceProcessor.Formalizer.New;
using Inforoom.PriceProcessor.Helpers;
using log4net;
using Inforoom.PriceProcessor;
using FileHelper=Common.Tools.FileHelper;

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
		private readonly Thread _thread;
		//����� ���������� ������� ����� 
		private DateTime? _abortingTime;

		private readonly PriceProcessLogger _log;
		private readonly ILog _logger = LogManager.GetLogger(typeof(PriceProcessThread));

		private IPriceFormalizer _workPrice;

		public PriceProcessThread(PriceProcessItem item, string prevErrorMessage)
		{
			StartDate = DateTime.UtcNow;
			ProcessItem = item;
			ProcessState = PriceProcessState.None;

			_log = new PriceProcessLogger(prevErrorMessage, item);
			_thread = new Thread(ThreadWork);
			_thread.Name = String.Format("PPT{0}", _thread.ManagedThreadId);
			_thread.Start();
		}
		/// <summary>
		/// ������� ����� ��� ������������, �� �� ���������
		/// </summary>
		public PriceProcessThread(PriceProcessItem item)
		{
			StartDate = DateTime.UtcNow;
			ProcessItem = item;
			ProcessState = PriceProcessState.None;

			_log = new PriceProcessLogger(String.Empty, item);
			_thread = new Thread(ThreadWork);
			_thread.Name = String.Format("PPT{0}", _thread.ManagedThreadId);
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
		public DateTime StartDate { get; private set; }

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
		public PriceProcessItem ProcessItem { get; private set; }

		public PriceProcessState ProcessState { get; private set; }

		/// <summary>
		/// ��������� ������������
		/// </summary>
		public void ThreadWork()
		{
			ProcessState = PriceProcessState.Begin;
			var allWorkTimeString = String.Empty;
			using(var cleaner = new FileCleaner())
			try
			{
				//��� ����� ��� ����������� � ���������� Base �������� ���: <PriceItemID> + <������������ ���������� �����>
				var outPriceFileName = FileHelper.NormalizeDir(Settings.Default.BasePath) + ProcessItem.PriceItemId + Path.GetExtension(ProcessItem.FilePath);
				//���������� ������������� ����� � �������� �������� ��������� �����
				var tempPath = Path.GetTempPath() + TID + "\\";
				//�������� ��� �����, ��� ��� ���� ��� ������������ �������� ('_')
				var tempFileName = tempPath + ProcessItem.PriceItemId + Path.GetExtension(ProcessItem.FilePath);
				cleaner.Watch(tempFileName);
				_logger.DebugFormat("�������� ����� �� ��������� ����� : {0}", ProcessItem.FilePath);

				ProcessState = PriceProcessState.CreateTempDirectory;

				//������� ���������� ��� ���������� ����� � �������� ���� ����
				if (Directory.Exists(tempPath))
					FileHelper.DeleteDir(tempPath);

				Directory.CreateDirectory(tempPath);

				try
				{
					ProcessState = PriceProcessState.CallValidate;
					_workPrice = PricesValidator.Validate(ProcessItem.FilePath, tempFileName, (uint)ProcessItem.PriceItemId);

					_workPrice.Downloaded = ProcessItem.Downloaded;
					_workPrice.InputFileName = ProcessItem.FilePath;

					ProcessState = PriceProcessState.CallFormalize;
					_workPrice.Formalize();

					FormalizeOK = true;
					_log.SuccesLog(_workPrice);
				}
				finally
				{
					var tsFormalize = DateTime.UtcNow.Subtract(StartDate);
					_log.FormSecs = Convert.ToInt64(tsFormalize.TotalSeconds);
					allWorkTimeString = tsFormalize.ToString();
				}

				ProcessState = PriceProcessState.FinalCopy;
				//���� ���� �� �����������, �� �� Inbound �� �� ��������� � ����� ������� ������������ ��� ���
				if (File.Exists(tempFileName))
					File.Copy(tempFileName, outPriceFileName, true);
				else
					File.Copy(ProcessItem.FilePath, outPriceFileName, true);
				var ft = DateTime.UtcNow;
				File.SetCreationTimeUtc(outPriceFileName, ft);
				File.SetLastWriteTimeUtc(outPriceFileName, ft);
				File.SetLastAccessTimeUtc(outPriceFileName, ft);
			}
			catch (ThreadAbortException e)
			{
				_log.ErrodLog(_workPrice, new Exception(Settings.Default.ThreadAbortError));
			}
			catch(Exception e)
			{
				if (e.InnerException is WarningFormalizeException || e is WarningFormalizeException) {
					_logger.Warn("������������ ����� ����� ��������� � ���������������", e);
					FormalizeOK = true;
				}
				else
				{
					_log.ErrodLog(_workPrice, e);
					Mailer.SendFromServiceToService("������ ��� ������������ ����� �����", e.ToString());
				}
			}
			finally
			{
				ProcessState = PriceProcessState.FinalizeThread;
				_logger.InfoFormat("����� ��������� ������ � ������� {0}: {1}.", ProcessItem.FilePath, allWorkTimeString);
				FormalizeEnd = true;
			}
		}
	}
}
