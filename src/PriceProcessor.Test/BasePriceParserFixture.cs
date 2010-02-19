using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Formalizer;
using NUnit.Framework;
using MySql.Data.MySqlClient;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class BasePriceParserFixture
	{
		const int catalogId = 13468;
		const int producerId = 1492;
		const int priceItemId = 688;
		const int priceCode = 5;

		private void PrepareTables(int priceCode, int catalogId, int producerId)
		{
			TestHelper.Execute(@"
delete from farm.Excludes
where PriceCode = {0} and CatalogId = {1}", priceCode, catalogId);
			TestHelper.Execute(@"delete from Catalogs.Assortment where CatalogId = {0} and ProducerId = {1}", catalogId, producerId);

			TestHelper.Execute(@"
delete from farm.Synonym where pricecode = {0} and synonym like '{1}';
insert into farm.Synonym(PriceCode, Synonym, ProductId) Values({0}, '{1}', {2});
insert into farm.UsedSynonymLogs(SynonymCode) Values(last_insert_id());",
				priceCode,
				"5 дней ванна д/ног смягчающая №10 пак. 25г  ",
				catalogId);
			TestHelper.Execute(@"
delete from farm.SynonymFirmCr where priceCode = {0} and synonym like '{1}';
insert into farm.SynonymFirmCr(PriceCode, Synonym, CodeFirmCr) Values({0}, '{1}', {2});
insert into farm.UsedSynonymFirmCrLogs(SynonymFirmCrCode) Values(last_insert_id());",
				priceCode,
				"Санкт-Петербургская ф.ф.",
				producerId);
			TestHelper.Execute("update catalogs.assortment set Checked = 0 where CatalogId = {0}", catalogId);			
		}

		[Test]
		public void Test_create_original_synonym_id()
		{
			var notCheckedProducerId = 3;
			var file = @"..\..\Data\688-create-net-assortment.txt";
			var rules = new DataTable();
			rules.ReadXml(String.Format(@"..\..\Data\{0}-assortment-rules.xml", priceItemId));

			// Каталожная запись не проверена, производитель проверен. Должны вставить в исключения
			PrepareTables(priceCode, catalogId, producerId);
			TestHelper.Execute(@"update catalogs.producers set Checked = 1 where Id = {0}", producerId);
			TestHelper.Formalize(typeof(DelimiterNativeTextParser1251), rules, file, priceItemId);
			var excludes = TestHelper.Fill(String.Format("select OriginalSynonymId from farm.Excludes where PriceCode = {0} and CatalogId = {1}", priceCode, catalogId));
			Assert.That(excludes.Tables[0].Rows.Count, Is.EqualTo(1));
			// Проверяем, что запись в исключениях создалась для нужного синонима
			var synonym = With.Connection(connection => MySqlHelper.ExecuteScalar(connection, 
					"select Synonym from farm.Synonym where SynonymCode = " + excludes.Tables[0].Rows[0]["OriginalSynonymId"].ToString())).ToString();
			Assert.That(synonym, Is.EqualTo("5 дней ванна д/ног смягчающая №10 пак. 25г  "));
		}

		[Test] 
		public void FormalizeAssortmentPriceTest()
		{
			// Внимание!!! Переименовывается таблица catalogs.Assortment. Блок finally должен отрабатывать
			TestFormalizeWithoutAssortmentInserting(
				@"..\..\Data\688-create-net-assortment.txt",
				String.Format(@"..\..\Data\{0}-assortmentprice-rules.xml", priceItemId));
		}

		[Test] 
		public void FormalizeHelpPriceTest()
		{
			// Внимание!!! Переименовывается таблица catalogs.Assortment. Блок finally должен отрабатывать
			TestFormalizeWithoutAssortmentInserting(
				@"..\..\Data\688-create-net-assortment.txt",
				String.Format(@"..\..\Data\{0}-helpprice-rules.xml", priceItemId));
		}

		private void TestFormalizeWithoutAssortmentInserting(string dataPath, string rulesPath)
		{  // Внимание!!! Переименовывается таблица catalogs.Assortment. Блок finally должен отрабатывать
			var rules = new DataTable();
			rules.ReadXml(rulesPath);

			TestHelper.Execute("RENAME TABLE catalogs.Assortment TO catalogs.Assortment_Backup;");
			TestHelper.Execute("CREATE TABLE catalogs.Assortment LIKE catalogs.Assortment_Backup;");
			try
			{
				// Каталожная запись не проверена, производитель проверен. Должны вставить в исключения
				PrepareTables(priceCode, catalogId, producerId);

				var countBefore = Convert.ToInt32(With.Connection(conn => MySqlHelper.ExecuteScalar(conn, "select count(*) from catalogs.Assortment")));
				Assert.AreEqual(0, countBefore, "Не подготовили таблицу Assortment.");
				TestHelper.Formalize(typeof(DelimiterNativeTextParser1251), rules, dataPath, priceItemId);

				var countAfter = Convert.ToInt32(With.Connection(conn => MySqlHelper.ExecuteScalar(conn, "select count(*) from catalogs.Assortment")));

				Assert.AreEqual(0, countAfter, "Добавили запись в Assortment, хотя и не должны были.");
			}
			finally
			{
				TestHelper.Execute(
@"DROP TABLE catalogs.Assortment; RENAME TABLE catalogs.Assortment_Backup TO catalogs.Assortment;");
			}
			
		}

		[Test]
		public void FormalizeProducerCost()
		{
			var fileName = @"formalize-producer-cost";
			var updateDate = DateTime.Now;
			var file = String.Format(@"..\..\Data\{0}-{1}.txt", priceItemId, fileName);

			var rules = new DataTable();
			rules.ReadXml(String.Format(@"..\..\Data\{0}-producer-cost-rules.xml", priceItemId));

			//Формализация прайс-листа
			TestHelper.Formalize(typeof(DelimiterNativeTextParser1251), rules, file, priceItemId);
			//Подсчет позиций в Core
			var cost = TestHelper.Fill(String.Format(@"
select * from usersettings.pricescosts pc where pc.PriceItemId = {0}", priceItemId));			
			var corePriceCode = Convert.ToInt64(cost.Tables[0].Rows[0]["PriceCode"]);


			With.Connection(connection => {
                var command = new MySqlCommand(@"
select count(*) from farm.Core0 core
  join farm.CoreCosts cc on core.Id = cc.Core_id and cc.Cost > core.ProducerCost
where PriceCode = ?PriceCode and ProducerCost is not null;", connection);
                command.Parameters.AddWithValue("?PriceCode", corePriceCode);
				var countCorePositions = Convert.ToUInt32(command.ExecuteScalar());
				Assert.That(countCorePositions, Is.EqualTo(14));
			});			
		}
	}
}
