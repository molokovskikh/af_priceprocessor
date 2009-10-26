using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Common.Tools;
using Inforoom.PriceProcessor.Formalizer;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture(Description = "тесты для проверки функциональности ассортимента")]
	public class AssortmentFixture
	{
		[Test]
		public void Formalize_test_price()
		{
			var priceItemId = 688;
			var file = String.Format(@"..\..\Data\{0}-assortment.txt", priceItemId);

			var rules = new DataTable();
			rules.ReadXml(String.Format(@"..\..\Data\{0}-assortment-rules.xml", priceItemId));

			var priceCode = 5;
			var excludeCatalogId = 87471;

			TestHelper.Execute(String.Format("delete from farm.Synonym where (PriceCode = {0}) and (Synonym = '{1}')", priceCode, "ОСТРОВИДКИ ПЛЮС С ЛЮТЕИНОМ КАПС. 710МГ №50 (БАД) super  "));

			TestHelper.Execute(String.Format("delete from farm.SynonymFirmCr where (PriceCode = {0}) and (Synonym = '{1}')", priceCode, "Фармстандарт (ICN) Лексредства г.Курск super"));
			var producerSynonyms = TestHelper.Fill(String.Format("select * from farm.SynonymFirmCr where (PriceCode = {0}) and (Synonym = '{1}')", priceCode, "Фармстандарт (ICN) Лексредства г.Курск super"));
			Assert.That(producerSynonyms.Tables[0].Rows.Count, Is.EqualTo(0), "имеются синонимы производителей для 'Фармстандарт (ICN) Лексредства г.Курск super'");

			TestHelper.Execute(String.Format("delete from farm.Excludes where (PriceCode = {0}) and (CatalogId = {1})", priceCode, excludeCatalogId));
			var excludes = TestHelper.Fill(String.Format("select * from farm.Excludes where (PriceCode = {0}) and (CatalogId = {1})", priceCode, excludeCatalogId));
			Assert.That(excludes.Tables[0].Rows.Count, Is.EqualTo(0), "имеются неизвестные исключения");

			TestHelper.Formalize(typeof(DelimiterNativeTextParser1251), rules, file, priceItemId);

			producerSynonyms = TestHelper.Fill(String.Format("select * from farm.SynonymFirmCr where (PriceCode = {0}) and (Synonym = '{1}')", priceCode, "Фармстандарт (ICN) Лексредства г.Курск super"));
			Assert.That(producerSynonyms.Tables[0].Rows.Count, Is.EqualTo(1), "ожидался синоним производителя для 'Фармстандарт (ICN) Лексредства г.Курск super'");
			var createdProducerSynonym = Convert.ToInt64(producerSynonyms.Tables[0].Rows[0]["SynonymFirmCrCode"]);

			var automaticSynonyms = TestHelper.Fill(String.Format("select * from farm.automaticProducerSynonyms where ProducerSynonymId = {0}", createdProducerSynonym));
			Assert.That(automaticSynonyms.Tables[0].Rows.Count, Is.EqualTo(1), "не были созданы автоматические синонимы");

			excludes = TestHelper.Fill(String.Format("select * from farm.Excludes where (PriceCode = {0}) and (CatalogId = {1})", priceCode, excludeCatalogId));
			Assert.That(excludes.Tables[0].Rows.Count, Is.EqualTo(1), "ожидалось исключение для CatalogId = {0}", excludeCatalogId);

			var unrexExp = TestHelper.Fill(String.Format("select * from farm.UnrecExp where (PriceItemId = {0})", priceItemId));
			var unrexExpTable = unrexExp.Tables[0];
			Assert.That(unrexExpTable.Rows.Count, Is.EqualTo(3), "не совпадает кол-во нераспознанных выражений");
			var drs = unrexExpTable.Select("ProducerSynonymId = " + createdProducerSynonym);
			Assert.That(drs.Length, Is.EqualTo(2), "не совпадает кол-во нераспознанных выражений с автоматически созданным синонимом");
			drs = unrexExpTable.Select("ProductSynonymId is null");
			Assert.That(drs.Length, Is.EqualTo(2), "не совпадает кол-во нераспознанных выражений по наименованию");
			drs = unrexExpTable.Select("ProducerSynonymId is null");
			Assert.That(drs.Length, Is.EqualTo(0), "не ожидались выражения без синонимов производителей");

			testUnrecExpPosition(
				unrexExpTable, 
				"(PriorProductId is null) and (PriorProducerId is not null) and (ProductSynonymId is null) and (ProducerSynonymId is not null)", 
				2);
			testUnrecExpPosition(
				unrexExpTable,
				"(PriorProductId is not null) and (PriorProducerId is null) and (ProductSynonymId is not null) and (ProducerSynonymId is not null)",
				1);
			testUnrecExpPosition(
				unrexExpTable,
				"(PriorProductId is null) and (PriorProducerId is null) and (ProductSynonymId is null) and (ProducerSynonymId is not null)",
				0);
		}

		private void testUnrecExpPosition(DataTable unrecExp, string filter, int status)		
		{
			var drs = unrecExp.Select(filter);
			Assert.That(drs.Length, Is.EqualTo(1), "не совпадает кол-во ожидаемых выражений");
			Assert.That(Convert.ToInt32(drs[0]["Status"]), Is.EqualTo(status), "неожидаемое значение статуса");
		}
	}
}
