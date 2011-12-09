using System;
using Common.MySql;
using NUnit.Framework;
using System.Data;
using Inforoom.PriceProcessor.Formalizer;
using MySql.Data.MySqlClient;
using PriceProcessor.Test.TestHelpers;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace PriceProcessor.Test
{
	[TestFixture(Description = "тесты для проверки функциональности обновления UsedSynonymLogs и UsedSynonymFirmCrLogs")]
    [Ignore("Починить")]
	public class UsedSynonymLogsFixture
	{
		private int priceItemId = 688;
		private long costCode;
		private long corePriceCode;
		private DateTime updateDate;

		private void FormalizePrice(string fileName, int waitingUpdatedSynonymLogs, int waitingUpdatedSynonymFirmCrLogs)
		{
			updateDate = DateTime.Now;
			var file = String.Format(@"..\..\Data\{0}-{1}.txt", priceItemId, fileName);

			var rules = new DataTable();
			rules.ReadXml(String.Format(@"..\..\Data\{0}-assortment-rules.xml", priceItemId));

			//Формализация прайс-листа
			TestHelper.Formalize(typeof(DelimiterNativeTextParser1251), rules, file, priceItemId);

			//Подсчет позиций в Core
			var cost = TestHelper.Fill(String.Format(
	"select * from usersettings.pricescosts pc where pc.PriceItemId = {0}",
	priceItemId));
			costCode = Convert.ToInt64(cost.Tables[0].Rows[0]["CostCode"]);
			corePriceCode = Convert.ToInt64(cost.Tables[0].Rows[0]["PriceCode"]);
			var core = TestHelper.Fill(String.Format(
				"select * from farm.Core0 c, farm.CoreCosts cc where (c.PriceCode = {0}) and (cc.Core_id = c.Id) and (cc.PC_CostCode = {1})",
				corePriceCode,
				costCode));
			Assert.That(core.Tables[0].Rows.Count, Is.EqualTo(14), "не совпадает кол-во предложений в Core");

			var updatedSynonymLogs = With.Connection<int>(connection =>
			{
				return Convert.ToInt32(
					MySqlHelper.ExecuteScalar(
					connection,
					@"
select 
  count(*) 
from 
  farm.Core0 c, 
  farm.UsedSynonymLogs usl, 
  farm.CoreCosts cc 
where 
    (c.PriceCode = ?corePriceCode) 
and (cc.Core_id = c.Id) 
and (cc.PC_CostCode = ?costCode) 
and (c.SynonymCode = usl.SynonymCode) 
and (usl.LastUsed > ?updateDate)",
					new MySqlParameter("?corePriceCode", corePriceCode),
					new MySqlParameter("?costCode", costCode),
					new MySqlParameter("?updateDate", updateDate))
					);
			});
			Assert.That(updatedSynonymLogs, Is.EqualTo(waitingUpdatedSynonymLogs), "не совпадает кол-во обновленных синонимов наименований в UsedSynonymLogs");

			var updatedSynonymFirmCrLogs = With.Connection<int>(connection =>
			{
				return Convert.ToInt32(
					MySqlHelper.ExecuteScalar(
					connection,
					@"
select 
  count(*) 
from 
  farm.Core0 c, 
  farm.UsedSynonymFirmCrLogs usl, 
  farm.CoreCosts cc 
where 
    (c.PriceCode = ?corePriceCode) 
and (cc.Core_id = c.Id) 
and (cc.PC_CostCode = ?costCode) 
and (c.SynonymFirmCrCode = usl.SynonymFirmCrCode) 
and (usl.LastUsed > ?updateDate)",
					new MySqlParameter("?corePriceCode", corePriceCode),
					new MySqlParameter("?costCode", costCode),
					new MySqlParameter("?updateDate", updateDate))
					);
			});
			Assert.That(updatedSynonymFirmCrLogs, Is.EqualTo(waitingUpdatedSynonymFirmCrLogs), "не совпадает кол-во обновленных синонимов производителей в UsedSynonymFirmCrLogs");
		}

		[Test(Description =	"обработка прайс-листа с несуществующим производителем"), Ignore]
		public void not_exists_producersynonym()
		{
			FormalizePrice("not_exists_producersynonym", 14, 12);
		}

		[Test(Description = "обработка прайс-листа с несуществующим производителем"), Ignore]
		public void empty_list_with_producersynonym()
		{
			FormalizePrice("empty_list_with_producersynonym", 14, 0);
		}
	}
}
