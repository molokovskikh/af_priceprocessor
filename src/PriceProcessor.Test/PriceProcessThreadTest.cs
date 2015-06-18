using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor;
using Inforoom.Formalizer;
using Test.Support;
using log4net.Appender;
using log4net.Config;
using NUnit.Framework;
using System.Threading;
using MySql.Data.MySqlClient;
using log4net;
using System.Data;
using System.Reflection;
using Common.MySql;
using Test.Support.Suppliers;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;


namespace PriceProcessor.Test
{
	[TestFixture]
	public class PriceProcessThreadTest
	{
		[Test(Description = "проверка корректности логирования при возникновении WarningFormalizeException")]
		public void CatchWarningFormalizeExceptionTest()
		{
			uint priceItemId = CatchWarningFormalizeExceptionTestPrepareData();
			var priceProcessItem = new PriceProcessItem(false, 0, null, priceItemId, @"Data\781.dbf", null);
			var priceProcessThread = new PriceProcessThread(priceProcessItem, String.Empty, false);
			var outPriceFileName = Path.Combine(Settings.Default.BasePath, priceProcessItem.PriceItemId + Path.GetExtension(priceProcessItem.FilePath));
			File.Delete(outPriceFileName);
			priceProcessThread.ThreadWork();
			Assert.False(String.IsNullOrEmpty(priceProcessThread.CurrentErrorMessage), "Отсутствует информация о произошедшем исключении");
			Assert.True(priceProcessThread.FormalizeOK, "Формализация закончилась с ошибкой");

			using (new SessionScope()) {
				var log = Inforoom.PriceProcessor.Models.FormLog.Queryable
					.Where(l => l.PriceItemId == priceItemId
						&& l.Host.Equals(Environment.MachineName)
						&& l.ResultId == (int?)FormResults.Error
						&& l.Addition.Equals(priceProcessThread.CurrentErrorMessage))
					.ToList();
				Assert.That(log.Count, Is.GreaterThan(0), "Информация о предупреждении отсутствует в БД");
			}
			//Проверяем, что копирование файла прошло успешно
			Assert.IsTrue(File.Exists(outPriceFileName));
		}

		private uint CatchWarningFormalizeExceptionTestPrepareData(PriceFormatType priceFormatId = PriceFormatType.NativeDbf, CostType priceCostType = CostType.MultiColumn)
		{
			var supplier = TestSupplier.Create();
			supplier.Disabled = true;
			var price = supplier.Prices[0];
			price.CostType = priceCostType;

			var item = price.Costs.First().PriceItem;
			var format = price.Costs.Single().PriceItem.Format;
			format.PriceFormat = priceFormatId;

			supplier.Save();
			price.Save();
			return item.Id;
		}

		[Test, Ignore("тестирование методов AbortThread и IsAbortingLong")]
		public void AbortingThreadTest()
		{
			var _priceProcessItem = new PriceProcessItem(false, 4596, null, 708, @"D:\Temp\Inbound0\708.dbf", null);
			var _priceProcessThread = new PriceProcessThread(_priceProcessItem, String.Empty);
			while ((_priceProcessThread.ThreadState != ThreadState.Running) && _priceProcessThread.ThreadIsAlive) {
				Thread.Sleep(500);
			}
			_priceProcessThread.AbortThread();
			Assert.That(!_priceProcessThread.IsAbortingLong, "Ошибка в расчете времени прерывания");
			while (!_priceProcessThread.FormalizeEnd && ((_priceProcessThread.ThreadState & ThreadState.Stopped) == 0))
				Thread.Sleep(500);
			Assert.That(!_priceProcessThread.IsAbortingLong, "Ошибка в расчете времени прерывания");
		}

		private enum CorruptDBThreadState
		{
			Init,
			CheckConnection,
			FirstSelect,
			InTransaction,
			Stopped
		}

		private class CorruptDBThread
		{
			private CorruptDBThreadState _state = CorruptDBThreadState.Init;
			private Thread _thread;

			private ILog _logger = LogManager.GetLogger(typeof(CorruptDBThread));

			public CorruptDBThreadState State
			{
				get { return _state; }
			}

			public Thread Thread
			{
				get { return _thread; }
			}

			private MySqlConnection GetConnection()
			{
				return new MySqlConnection(ConnectionHelper.GetConnectionString());
			}

			public CorruptDBThread()
			{
				_thread = new Thread(ThreadMethod);
				_thread.Name = "PPT" + _thread.ManagedThreadId;
				_thread.Start();
				_logger.Info("Start");
			}

			/// <summary>
			/// Проверка connection на попытку выборки из select now(). Если выборка не будет успешной, то генерируем ошибку.
			/// </summary>
			/// <param name="myconn"></param>
			private void CheckConnection(MySqlConnection myconn)
			{
				if (myconn.State != ConnectionState.Open)
					myconn.Open();
				try {
					DataSet dsNowTime = MySqlHelper.ExecuteDataset(myconn, "select now()");
					if (!((dsNowTime.Tables.Count == 1) && (dsNowTime.Tables[0].Rows.Count == 1))) {
						//Попытка получить время создания connection
						DateTime? creationTime = null;
						FieldInfo driverField = myconn.GetType().GetField("driver", BindingFlags.Instance | BindingFlags.NonPublic);
						object driverInternal = driverField.GetValue(myconn);
						if (driverInternal != null) {
							FieldInfo creationTimeField = driverInternal.GetType().GetField("creationTime", BindingFlags.Instance | BindingFlags.NonPublic);
							creationTime = (DateTime?)creationTimeField.GetValue(driverInternal);
						}

						//Пытаемся получить InnoDBStatus
						bool InnoDBByConnection = false;
						string InnoDBStatus = String.Empty;
						DataSet dsStatus = MySqlHelper.ExecuteDataset(myconn, "show engine innodb status");
						if ((dsStatus.Tables.Count == 1) && (dsStatus.Tables[0].Rows.Count == 1) && (dsStatus.Tables[0].Columns.Contains("Status"))) {
							InnoDBStatus = dsStatus.Tables[0].Rows[0]["Status"].ToString();
							InnoDBByConnection = true;
						}
						if (!InnoDBByConnection) {
							var drInnoDBStatus = MySqlHelper.ExecuteDataRow(ConnectionHelper.GetConnectionString(), "show engine innodb status");
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

						throw new Exception("При попытке выборки из select now() не получили записей. Перезапустите PriceProcessor.");
					}
				}
				finally {
					myconn.Close();
				}
			}

			private void ThreadMethod()
			{
				try {
					MySqlConnection _connection = GetConnection();
					_logger.Info("GetConnection()");

					_state = CorruptDBThreadState.CheckConnection;
					CheckConnection(_connection);

					_connection.Open();
					try {
						MySqlTransaction _selectTransaction = _connection.BeginTransaction();
						_state = CorruptDBThreadState.FirstSelect;
						try {
							DataSet dsColumns = MySqlHelper.ExecuteDataset(_connection, "select * from information_schema.`COLUMNS`");
							Thread.Sleep(500);
							DataSet dsColumnsPrivileges = MySqlHelper.ExecuteDataset(_connection, "select * from information_schema.COLUMN_PRIVILEGES");
						}
						finally {
							_selectTransaction.Commit();
						}
						_logger.Info("FirstSelect");
					}
					finally {
						_connection.Close();
					}

					Thread.Sleep(1000);

					_connection.Open();
					try {
						MySqlTransaction _brokenTransaction = _connection.BeginTransaction();
						_state = CorruptDBThreadState.InTransaction;
						try {
							DataSet dsColumns = MySqlHelper.ExecuteDataset(_connection, "select * from information_schema.`COLUMNS`");

							Thread.Sleep(3000);

							_logger.Fatal("Дошли до отката транзакции, чего быть не должно");
							_brokenTransaction.Rollback();
						}
						catch (Exception onTransaction) {
							_logger.Error("Ошибка в транзакции", onTransaction);
							if (_brokenTransaction != null)
								try {
									_logger.Info("Начало отката");
									_brokenTransaction.Rollback();
									_logger.Info("Откат завершен");
								}
								catch (Exception onRollback) {
									_logger.Error("Ошибка при откате", onRollback);
								}
							throw;
						}
					}
					finally {
						_connection.Close();
					}
				}
				catch (Exception exception) {
					_logger.Error("Ошибка в нитке", exception);
				}
				finally {
					_state = CorruptDBThreadState.Stopped;
					_logger.Info("Stop");
				}
			}
		}

		[Test, Ignore("в результате действий подвреждаем MySqlConnection, что в нем запросы перестают возвращать данные, запрещаем выполнение, т.к. тест подвисает приложение")]
		public void CorruptConnectionTest()
		{
			BasicConfigurator.Configure(
				new TraceAppender() {
					Layout = new log4net.Layout.PatternLayout("%date{ABSOLUTE} [%-5thread] %-5level %logger{1} %ndc - %message%newline")
				});
			ILog _logger = LogManager.GetLogger(typeof(PriceProcessThreadTest));

			List<CorruptDBThread> _threads = new List<CorruptDBThread>();

			_logger.Info("Запустили тест");
			_threads.Add(new CorruptDBThread());
			_threads.Add(new CorruptDBThread());

			int _stopCount = 0;
			while (_stopCount < 5) {
				Thread.Sleep(100);

				for (int i = _threads.Count - 1; i >= 0; i--) {
					if (_threads[i].State == CorruptDBThreadState.Stopped) {
						string _deletedThreadName = _threads[i].Thread.Name;
						_threads.RemoveAt(i);
						_stopCount++;
						_logger.InfoFormat("Удалили нитку {0}", _deletedThreadName);
					}
					else if ((_threads[i].State == CorruptDBThreadState.InTransaction) &&
						((_threads[i].Thread.ThreadState & ThreadState.AbortRequested) == 0)) {
						_threads[i].Thread.Abort();
						_logger.InfoFormat("Вызвали Abort() для нитки {0}", _threads[i].Thread.Name);
					}
					else if ((_threads[i].State == CorruptDBThreadState.InTransaction) &&
						((_threads[i].Thread.ThreadState & ThreadState.AbortRequested) > 0) &&
						((_threads[i].Thread.ThreadState & ThreadState.WaitSleepJoin) > 0)) {
						_threads[i].Thread.Interrupt();
						_logger.InfoFormat("Вызвали Interrupt() для нитки {0}", _threads[i].Thread.Name);
					}
				}

				while (_threads.Count < 2)
					_threads.Add(new CorruptDBThread());
			}

			_logger.InfoFormat("Произвели останов {0} раз", _stopCount);

			_stopCount = 0;
			while (_stopCount < 5) {
				Thread.Sleep(100);

				for (int i = _threads.Count - 1; i >= 0; i--) {
					if (_threads[i].State == CorruptDBThreadState.Stopped) {
						string _deletedThreadName = _threads[i].Thread.Name;
						_threads.RemoveAt(i);
						_stopCount++;
						_logger.InfoFormat("Удалили нитку {0}", _deletedThreadName);
					}
				}

				while (_threads.Count < 1)
					_threads.Add(new CorruptDBThread());
			}

			_logger.Info("Закончили тест");
		}
	}
}