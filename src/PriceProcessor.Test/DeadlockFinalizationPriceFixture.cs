using System;
using System.Collections.Generic;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Formalizer;
using NUnit.Framework;
using Inforoom.Formalizer;
using System.Reflection;
using System.Data;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Threading;

namespace PriceProcessor.Test
{
	[TestFixture(Description = "тесты для проверки обработки Deadlock'ов при финализации прайс-листов в базе")]
	public class DeadlockFinalizationPriceFixture
	{
		string file = @"..\..\Data\688-deadlock.txt";
		int priceItemId = 688;
		DataTable rules = new DataTable();
		long priceCode;
		long costCode;
		int etalonRowCount;

		[SetUp]
		public void prepare_price_formalization()
		{
			rules.ReadXml(String.Format(@"..\..\Data\{0}-assortment-rules.xml", priceItemId));

			var priceItemInfo = TestHelper.Fill(String.Format(
				" select pc.PriceCode, pc.CostCode, pd.CostType " +
			    " from usersettings.pricescosts pc, usersettings.pricesdata pd " + 
				" where (pc.PriceItemId = {0}) and (pd.PriceCode = pc.PriceCode)",
				priceItemId));
			var costType = Convert.ToByte(priceItemInfo.Tables[0].Rows[0]["CostType"]);
			costCode = Convert.ToInt64(priceItemInfo.Tables[0].Rows[0]["CostCode"]);
			priceCode = Convert.ToInt64(priceItemInfo.Tables[0].Rows[0]["PriceCode"]);
			Assert.That(costType, Is.EqualTo(1), "Тест поломан. Тип ценовой колонки для PriceCode = {0} не соответствует.", priceCode);

			//удаляем записи, т.к. будет формализовать прайс-лист для эталона
			TestHelper.Execute(@"
delete
  farm.Core0
from
  farm.CoreCosts,
  farm.Core0
where
    CoreCosts.Core_Id = Core0.Id
and Core0.PriceCode = {0}
and CoreCosts.PC_CostCode = {1}"
				,
				priceCode, 
				costCode);
			//Сбрасываем значение распознанных позиций
			TestHelper.Execute("update usersettings.PriceItems set RowCount = 0 where Id = {0}", priceItemId);

			//Формализация прайс-листа
			TestHelper.Formalize(typeof(DelimiterNativeTextParser1251), rules, file, priceItemId);

			var core = TestHelper.Fill(String.Format(@"
select
  *
from
  farm.CoreCosts,
  farm.Core0
where
    CoreCosts.Core_Id = Core0.Id
and Core0.PriceCode = {0}
and CoreCosts.PC_CostCode = {1}",
				priceCode,
				costCode));
			etalonRowCount = Convert.ToInt32(
				MySqlHelper.ExecuteScalar(
					//ConfigurationManager.ConnectionStrings["DB"].ConnectionString,
                    Literals.ConnectionString(),
					"select RowCount from usersettings.PriceItems where Id = ?PriceItemId",
					new MySqlParameter("?PriceItemId", priceItemId)));
			Assert.That(etalonRowCount, Is.EqualTo(core.Tables[0].Rows.Count), "Не совпадает кол-во позиций в Core и RowCount в PriceItems.");

			//Обновляем Quantity, чтобы при синхронизации была попытка обновить Core
			TestHelper.Execute(@"
update
  farm.CoreCosts,
  farm.Core0
set
  Core0.CodeCr = 'тест',
  Core0.Quantity = 10
where
    CoreCosts.Core_Id = Core0.Id
and Core0.PriceCode = {0}
and CoreCosts.PC_CostCode = {1}"
				, 
				priceCode,
				costCode);
		}

		internal class DeadlockThread
		{
			long _priceCode;
			long _costCode;

			public string ThreadException;
			public Thread thread;

			public DeadlockThread(long priceCode, long costCode)
			{
				_priceCode = priceCode;
				_costCode = costCode;
				thread = new Thread(ThreadMethod);
				thread.Start();
			}

			private void ThreadMethod()
			{
				try
				{
					//using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString))
                    using (var connection = new MySqlConnection(Literals.ConnectionString()))
					{
						connection.Open();

						var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
						try
						{
							/*
							 * Как заблокировать записи на изменения: http://dev.mysql.com/doc/refman/5.0/en/innodb-locking-reads.html
							 * Под статьей комментарий:
							 * If you just want to lock a bunch of rows, without fetching any data, you can group them together using a dummy GROUP BY clause.
							 * SELECT 1 FROM sometable WHERE somecondition GROUP BY 1 FOR UPDATE;
							 */
							var command = new MySqlCommand(@"
SELECT 
1
from
  farm.CoreCosts,
  farm.Core0
where
    CoreCosts.Core_Id = Core0.Id
and Core0.PriceCode = ?PriceCode
and CoreCosts.PC_CostCode = ?CostCode
group by 1
for update"
								,
								connection,
								transaction);
							command.Parameters.AddWithValue("?PriceCode", _priceCode);
							command.Parameters.AddWithValue("?CostCode", _costCode);

							command.ExecuteScalar();

							//Надо спать какое-то время, чтобы нитка формализации попала в Deadlock
							Thread.Sleep(180000);

							transaction.Commit();
						}
						catch
						{
							transaction.Rollback();
							throw;
						}
					}
				}
				catch (Exception exception)
				{
					ThreadException = exception.ToString();
				}
			}
		}

		[Test(Description = "проверяем обработку deadlock'ов при использовании прайс-листом механизма синхронизации (update)"), Ignore]
		public void test_deadlock_on_update()
		{
			var deadlockThread = new DeadlockThread(priceCode, costCode);
			int parserLockCount;

			//using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString))
            using (var connection = new MySqlConnection(Literals.ConnectionString()))
			{
				
				var parser = new DelimiterNativeTextParser1251(file, connection, rules);

				//Добавляем выбранный прайс-лист в список прайс-листов, которые обновляются с помощью Update
				foreach (var field in typeof(BasePriceParser).GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic))
					if (field.Name == "priceCodesUseUpdate")
					{
						List<long> priceList = (List<long>)field.GetValue(parser);
						if (!priceList.Contains(priceCode))
							priceList.Add(priceCode);
					}

				parser.Formalize();
				parserLockCount = parser.maxLockCount;
			}

			//Ждем, пока закончится нитка
			deadlockThread.thread.Join();
			Assert.True(String.IsNullOrEmpty(deadlockThread.ThreadException), "В блокирующей нитке возникла ошибка: {0}", deadlockThread.ThreadException);

			Assert.That(parserLockCount, Is.GreaterThan(2), "При формализации прайс-листа не было Deadlock'ов в базе данных");

			//Проверяем совпадение значения формализованных позиций в PriceItems.RowCount
			var currentRowCount = Convert.ToInt32(
				MySqlHelper.ExecuteScalar(
					//ConfigurationManager.ConnectionStrings["DB"].ConnectionString,
                    Literals.ConnectionString(),
					"select RowCount from usersettings.PriceItems where Id = ?PriceItemId",
					new MySqlParameter("?PriceItemId", priceItemId)));
			Assert.That(currentRowCount, Is.EqualTo(etalonRowCount), "Не совпадает новое значение RowCount и эталлонное значение RowCount в PriceItems.");

			//Проверяем, что кол-во позиций в Core должно совпадать с эталонным значением
			var core = TestHelper.Fill(String.Format(@"
select
  *
from
  farm.CoreCosts,
  farm.Core0
where
    CoreCosts.Core_Id = Core0.Id
and Core0.PriceCode = {0}
and CoreCosts.PC_CostCode = {1}",
				priceCode,
				costCode));
			Assert.That(core.Tables[0].Rows.Count, Is.EqualTo(etalonRowCount), "Не совпадает кол-во позиций в Core и эталлонное значение RowCount в PriceItems.");

		}

		[Test(Description = "проверяем обработку deadlock'ов при использовании insert: полного удаления позиций, а потом вставка"), Ignore]
		public void test_deadlock_on_insert()
		{
			var deadlockThread = new DeadlockThread(priceCode, costCode);
			int parserLockCount;

			//using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString))
            using (var connection = new MySqlConnection(Literals.ConnectionString()))
			{

				var parser = new DelimiterNativeTextParser1251(file, connection, rules);

				//Удаляем выбранный прайс-лист из списока прайс-листов, которые обновляются с помощью Update, если он там есть
				foreach (var field in typeof(BasePriceParser).GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic))
					if (field.Name == "priceCodesUseUpdate")
					{
						List<long> priceList = (List<long>)field.GetValue(parser);
						if (priceList.Contains(priceCode))
							priceList.Remove(priceCode);
					}

				parser.Formalize();
				parserLockCount = parser.maxLockCount;
			}

			//Ждем, пока закончится нитка
			deadlockThread.thread.Join();
			Assert.True(String.IsNullOrEmpty(deadlockThread.ThreadException), "В блокирующей нитке возникла ошибка: {0}", deadlockThread.ThreadException);

			Assert.That(parserLockCount, Is.GreaterThan(2), "При формализации прайс-листа не было Deadlock'ов в базе данных");

			//Проверяем совпадение значения формализованных позиций в PriceItems.RowCount
			var currentRowCount = Convert.ToInt32(
				MySqlHelper.ExecuteScalar(
					//ConfigurationManager.ConnectionStrings["DB"].ConnectionString,
                    Literals.ConnectionString(),
					"select RowCount from usersettings.PriceItems where Id = ?PriceItemId",
					new MySqlParameter("?PriceItemId", priceItemId)));
			Assert.That(currentRowCount, Is.EqualTo(etalonRowCount), "Не совпадает новое значение RowCount и эталлонное значение RowCount в PriceItems.");

			//Проверяем, что кол-во позиций в Core должно совпадать с эталонным значением
			var core = TestHelper.Fill(String.Format(@"
select
  *
from
  farm.CoreCosts,
  farm.Core0
where
    CoreCosts.Core_Id = Core0.Id
and Core0.PriceCode = {0}
and CoreCosts.PC_CostCode = {1}",
				priceCode,
				costCode));
			Assert.That(core.Tables[0].Rows.Count, Is.EqualTo(etalonRowCount), "Не совпадает кол-во позиций в Core и эталлонное значение RowCount в PriceItems.");

		}


	}
}
