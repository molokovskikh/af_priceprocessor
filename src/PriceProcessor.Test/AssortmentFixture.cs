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

			//удаляем синоним наименования
			TestHelper.Execute(
				String.Format(
					"delete from farm.Synonym where (PriceCode = {0}) and (Synonym = '{1}')", 
					priceCode, 
					"ОСТРОВИДКИ ПЛЮС С ЛЮТЕИНОМ КАПС. 710МГ №50 (БАД) super  "));

			//удаляем синоним производителя 'Фармстандарт (ICN) Лексредства г.Курск super'
			TestHelper.Execute(
				String.Format(
					"delete from farm.SynonymFirmCr where (PriceCode = {0}) and (Synonym = '{1}')", 
					priceCode, 
					"Фармстандарт (ICN) Лексредства г.Курск super"));
			var producerSynonyms = TestHelper.Fill(
				String.Format(
					"select * from farm.SynonymFirmCr where (PriceCode = {0}) and (Synonym = '{1}')", 
					priceCode, 
					"Фармстандарт (ICN) Лексредства г.Курск super"));
			Assert.That(producerSynonyms.Tables[0].Rows.Count, Is.EqualTo(0), "имеются синонимы производителей для 'Фармстандарт (ICN) Лексредства г.Курск super'");

			//Добавляем синоним производителя
			//Фармстандарт (ICN) Лексредства г.Курск super-puper
			TestHelper.Execute(
				String.Format(
					"delete from farm.SynonymFirmCr where (PriceCode = {0}) and (Synonym = '{1}')",
					priceCode,
					"Фармстандарт (ICN) Лексредства г.Курск super-puper"));
			TestHelper.Execute(
				String.Format(
					"insert into farm.SynonymFirmCr (PriceCode, Synonym) values ({0}, '{1}')",
					priceCode,
					"Фармстандарт (ICN) Лексредства г.Курск super-puper"));
			producerSynonyms = TestHelper.Fill(
							String.Format(
								"select * from farm.SynonymFirmCr where (PriceCode = {0}) and (Synonym = '{1}')",
								priceCode,
								"Фармстандарт (ICN) Лексредства г.Курск super-puper"));
			var automaticPuperId = Convert.ToInt64(producerSynonyms.Tables[0].Rows[0]["SynonymFirmCrCode"]);
			Assert.That(producerSynonyms.Tables[0].Rows.Count, Is.EqualTo(1), "нет синонима производителя для 'Фармстандарт (ICN) Лексредства г.Курск super-puper'");
			TestHelper.Execute(
				String.Format(
					"insert into farm.AutomaticProducerSynonyms (ProducerSynonymId) values ({0})",
					automaticPuperId));

			//удаляем исключения
			TestHelper.Execute(
				String.Format(
					"delete from farm.Excludes where (PriceCode = {0}) and (CatalogId = {1})", 
					priceCode, 
					excludeCatalogId));
			var excludes = TestHelper.Fill(
				String.Format(
					"select * from farm.Excludes where (PriceCode = {0}) and (CatalogId = {1})", 
					priceCode, 
					excludeCatalogId));
			Assert.That(excludes.Tables[0].Rows.Count, Is.EqualTo(0), "имеются неизвестные исключения");

			//Формализация прайс-листа
			TestHelper.Formalize(typeof(DelimiterNativeTextParser1251), rules, file, priceItemId);

			producerSynonyms = TestHelper.Fill(String.Format(
				"select * from farm.SynonymFirmCr where (PriceCode = {0}) and (Synonym = '{1}')", 
				priceCode, 
				"Фармстандарт (ICN) Лексредства г.Курск super"));
			Assert.That(producerSynonyms.Tables[0].Rows.Count, Is.EqualTo(0), "синоним производителя для 'Фармстандарт (ICN) Лексредства г.Курск super' не должен быть создан");

			excludes = TestHelper.Fill(String.Format("select * from farm.Excludes where (PriceCode = {0}) and (CatalogId = {1})", priceCode, excludeCatalogId));
			Assert.That(excludes.Tables[0].Rows.Count, Is.EqualTo(1), "ожидалось исключение для CatalogId = {0}", excludeCatalogId);

			var unrexExp = TestHelper.Fill(String.Format("select * from farm.UnrecExp where (PriceItemId = {0})", priceItemId));

			var unrexExpTable = unrexExp.Tables[0];
			Assert.That(unrexExpTable.Rows.Count, Is.EqualTo(5), "не совпадает кол-во нераспознанных выражений");

			var drs = unrexExpTable.Select("ProducerSynonymId = " + automaticPuperId);
			Assert.That(drs.Length, Is.EqualTo(2), "не совпадает кол-во нераспознанных выражений с автоматически созданным синонимом");

			drs = unrexExpTable.Select("ProductSynonymId is null");
			Assert.That(drs.Length, Is.EqualTo(3), "не совпадает кол-во нераспознанных выражений по наименованию");

			drs = unrexExpTable.Select("ProducerSynonymId is null");
			Assert.That(drs.Length, Is.EqualTo(2), "не кол-во нераспознанных выражений без синонимов производителей");


			//Проверка установленных статусов

			//Если сопоставлено по производителю с известным ProducerId
			testUnrecExpPosition(
				unrexExpTable, 
				"(PriorProductId is null) and (PriorProducerId is not null) and (ProductSynonymId is null) and (ProducerSynonymId is not null)", 
				2);

			//Сопосталевно по наименованию, но получен новый производитель
			testUnrecExpPosition(
				unrexExpTable,
				"(PriorProductId is not null) and (PriorProducerId is null) and (ProductSynonymId is not null) and (ProducerSynonymId is null)",
				1);

			//Несопоставлено по наименованию и производителю
			testUnrecExpPosition(
				unrexExpTable,
				"(PriorProductId is null) and (PriorProducerId is null) and (ProductSynonymId is null) and (ProducerSynonymId is null)",
				0);

			//Сопосталевно по наименованию, но сопоставлено с автоматическим синонимом производителя
			testUnrecExpPosition(
				unrexExpTable,
				"(PriorProductId is not null) and (PriorProducerId is null) and (ProductSynonymId is not null) and (ProducerSynonymId is not null)",
				1);

			//Несопоставлено по наименованию и сопоставлено с автоматическим синонимом производителя
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
