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
			var catalogId = 13468;
			var producerId = 1492;
			var notCheckedProducerId = 3;
			var file = @"..\..\Data\688-create-net-assortment.txt";
			var priceItemId = 688;
			var priceCode = 5;
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
	}
}
