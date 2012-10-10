﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using Castle.ActiveRecord;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;
using Inforoom.PriceProcessor.Models;
using NHibernate;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test
{
	public class FakeReader : IReader
	{
		public IEnumerable<FormalizationPosition> Read()
		{
			throw new NotImplementedException();
		}

		public List<CostDescription> CostDescriptions { get; set; }

		public IEnumerable<Customer> Settings()
		{
			throw new NotImplementedException();
		}

		public void SendWarning(PriceLoggingStat stat)
		{
			throw new NotImplementedException();
		}
	}

	[TestFixture(Description = "тесты для проверки функциональности ассортимента")]
	public class AssortmentFixture
	{
		[Test]
		public void Create_synonym_whith_producer_if_this_position_not_in_monobrend()
		{
			var producerSynonyms = new DataTable();
			producerSynonyms.Columns.Add("OriginalSynonym", typeof(string));
			producerSynonyms.Columns.Add("Synonym", typeof(string));
			producerSynonyms.Columns.Add("SynonymFirmCrCode", typeof(Int64));
			producerSynonyms.Columns.Add("IsAutomatic", typeof(bool));
			producerSynonyms.Columns.Add("CodeFirmCr", typeof(UInt32));
			producerSynonyms.Columns.Add("InternalProducerSynonymId");
			producerSynonyms.Columns["InternalProducerSynonymId"].AutoIncrement = true;

			var resolver = new ProducerResolver(null, new FormalizeStats(), null, producerSynonyms);
			var position = new FormalizationPosition {
				Pharmacie = true,
				FirmCr = "TestFirm",
				CatalogId = 777,
				Status = UnrecExpStatus.NameForm,
				Core = new NewCore()
			};
			var assortiment = new DataTable();
			assortiment.Columns.Add("Id", typeof(uint));
			assortiment.Columns.Add("CatalogId", typeof(uint));
			assortiment.Columns.Add("ProducerId", typeof(uint));
			assortiment.Columns.Add("Checked", typeof(bool));
			var newAssort = assortiment.NewRow();
			newAssort["Id"] = 77;
			newAssort["CatalogId"] = 777;
			newAssort["ProducerId"] = 111;
			newAssort["Checked"] = true;
			assortiment.Rows.Add(newAssort);
			resolver.Assortment = assortiment;
			var monobrendAssortiment = new DataTable();
			monobrendAssortiment.Columns.Add("Id", typeof(uint));
			monobrendAssortiment.Columns.Add("CatalogId", typeof(uint));
			resolver.MonobrendAssortment = monobrendAssortiment;
			resolver.ResolveProducer(position);

			Assert.IsNotNull(position.Core.CreatedProducerSynonym);
			Assert.That(position.Core.CreatedProducerSynonym["CodeFirmCr"], Is.EqualTo(DBNull.Value));
			Assert.IsNotNull(position.Core.CreatedProducerSynonym["IsAutomatic"] = 1);
		}
		[Test]
		public void Create_synonym_whith_producer_if_this_position_in_monobrend()
		{
			var producerSynonyms = new DataTable();
			producerSynonyms.Columns.Add("OriginalSynonym", typeof(string));
			producerSynonyms.Columns.Add("Synonym", typeof(string));
			producerSynonyms.Columns.Add("SynonymFirmCrCode", typeof(Int64));
			producerSynonyms.Columns.Add("IsAutomatic", typeof(bool));
			producerSynonyms.Columns.Add("CodeFirmCr", typeof(UInt32));
			producerSynonyms.Columns.Add("InternalProducerSynonymId");
			producerSynonyms.Columns["InternalProducerSynonymId"].AutoIncrement = true;

			var resolver = new ProducerResolver(null, new FormalizeStats(), null, producerSynonyms);
			var position = new FormalizationPosition {
				Pharmacie = true,
				FirmCr = "TestFirm",
				CatalogId = 777,
				Status = UnrecExpStatus.NameForm,
				Core = new NewCore()
			};
			var assortiment = new DataTable();
			assortiment.Columns.Add("Id", typeof(uint));
			assortiment.Columns.Add("CatalogId", typeof(uint));
			assortiment.Columns.Add("ProducerId", typeof(uint));
			assortiment.Columns.Add("Checked", typeof(bool));
			var newAssort = assortiment.NewRow();
			newAssort["Id"] = 77;
			newAssort["CatalogId"] = 777;
			newAssort["ProducerId"] = 111;
			newAssort["Checked"] = true;
			assortiment.Rows.Add(newAssort);
			resolver.Assortment = assortiment;

			var monobrendAssortiment = new DataTable();
			monobrendAssortiment.Columns.Add("Id", typeof(uint));
			monobrendAssortiment.Columns.Add("CatalogId", typeof(uint));
			var newMbAssort = monobrendAssortiment.NewRow();
			newMbAssort["Id"] = 77;
			newMbAssort["CatalogId"] = 777;
			monobrendAssortiment.Rows.Add(newMbAssort);
			resolver.MonobrendAssortment = monobrendAssortiment;

			resolver.ResolveProducer(position);

			Assert.IsNotNull(position.Core.CreatedProducerSynonym);
			Assert.That(position.Core.CreatedProducerSynonym["CodeFirmCr"], Is.EqualTo(111u));
			Assert.IsFalse(Convert.ToBoolean(position.Core.CreatedProducerSynonym["IsAutomatic"]));
		}

		[Test, Ignore]
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
					"#"));
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

			TestHelper.Execute("delete from catalogs.assortment where CatalogId = {0} and ProducerId = {1}", excludeCatalogId, 402);
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
			TestHelper.Execute("update Catalogs.Assortment set checked = 1 where CatalogId = {0} ", excludeCatalogId);

			//Формализация прайс-листа
			TestHelper.FormalizeOld(typeof(DelimiterNativeTextParser1251), rules, file, priceItemId);

			var cost = TestHelper.Fill(String.Format(
				"select * from usersettings.pricescosts pc where pc.PriceItemId = {0}",
				priceItemId));
			var costCode = Convert.ToInt64(cost.Tables[0].Rows[0]["CostCode"]);
			var corePriceCode = Convert.ToInt64(cost.Tables[0].Rows[0]["PriceCode"]);
			var core = TestHelper.Fill(String.Format(
				"select * from farm.Core0 c, farm.CoreCosts cc where (c.PriceCode = {0}) and (cc.Core_id = c.Id) and (cc.PC_CostCode = {1})",
				corePriceCode,
				costCode));
			Assert.That(core.Tables[0].Rows.Count, Is.EqualTo(18), "не совпадает кол-во предложений в Core");

			producerSynonyms = TestHelper.Fill(String.Format(
				"select * from farm.SynonymFirmCr where (PriceCode = {0}) and (Synonym = '{1}')",
				priceCode,
				"Фармстандарт (ICN) Лексредства г.Курск super"));
			Assert.That(producerSynonyms.Tables[0].Rows.Count, Is.EqualTo(1), "синоним производителя для 'Фармстандарт (ICN) Лексредства г.Курск super' не создан");

			excludes = TestHelper.Fill(String.Format("select * from farm.Excludes where (PriceCode = {0}) and (CatalogId = {1})", priceCode, excludeCatalogId));
			Assert.That(excludes.Tables[0].Rows.Count, Is.EqualTo(1), "ожидалось исключение для CatalogId = {0}", excludeCatalogId);
		}

		[Test, Ignore("Ассортимент временно не создается, а может и не временно")]
		public void Create_new_assortment_if_product_not_checked()
		{
			var catalogId = 13468;
			var producerId = 1492;

			var file = @"..\..\Data\688-create-net-assortment.txt";
			var priceItemId = 688;
			var priceCode = 5;

			var rules = new DataTable();
			rules.ReadXml(String.Format(@"..\..\Data\{0}-assortment-rules.xml", priceItemId));

			TestHelper.Execute(@"
delete from farm.Excludes
where PriceCode = {0} and CatalogId = {1}",
				priceCode, catalogId, producerId);
			TestHelper.Execute("delete from Catalogs.Assortment where CatalogId = {0} and ProducerId = {1}",
				catalogId, producerId);
			TestHelper.Execute(@"
delete from farm.Synonym where pricecode = {0} and synonym like '{1}';
insert into farm.Synonym(PriceCode, Synonym, ProductId) Values({0}, '{1}', {2});
insert into farm.UsedSynonymLogs(SynonymCode) Values(last_insert_id());",
				priceCode,
				"5 дней ванна д/ног смягчающая №10 пак. 25г  ",
				13468);
			TestHelper.Execute(@"
delete from farm.SynonymFirmCr where priceCode = {0} and synonym like '{1}';
insert into farm.SynonymFirmCr(PriceCode, Synonym, CodeFirmCr) Values({0}, '{1}', {2});
insert into farm.UsedSynonymFirmCrLogs(SynonymFirmCrCode) Values(last_insert_id());",
				priceCode,
				"Санкт-Петербургская ф.ф.",
				producerId);

			TestHelper.FormalizeOld(typeof(DelimiterNativeTextParser1251), rules, file, priceItemId);
			var assortment = TestHelper.Fill(String.Format("select * from catalogs.assortment where CatalogId = {0} and ProducerId = {1}", catalogId, producerId));
			Assert.That(assortment.Tables[0].Rows.Count, Is.EqualTo(1));
			Assert.That(assortment.Tables[0].Rows[0]["ProducerId"], Is.EqualTo(producerId));
			Assert.That(assortment.Tables[0].Rows[0]["CatalogId"], Is.EqualTo(catalogId));
		}

		private void PrepareTablesCreateAssortmentOrExcludesEntry(int priceCode, int catalogId, int producerId)
		{
			TestHelper.Execute(@"
delete from farm.Excludes
where PriceCode = {0} and CatalogId = {1}",
				priceCode, catalogId);
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

		[Test, Ignore]
		public void Test_create_assortment_or_excludes_entry()
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
			PrepareTablesCreateAssortmentOrExcludesEntry(priceCode, catalogId, producerId);
			TestHelper.Execute(@"update catalogs.producers set Checked = 1 where Id = {0}", producerId);
			TestHelper.FormalizeOld(typeof(DelimiterNativeTextParser1251), rules, file, priceItemId);
			var assortment = TestHelper.Fill(String.Format("select * from catalogs.assortment where CatalogId = {0} and ProducerId = {1}", catalogId, producerId));
			Assert.That(assortment.Tables[0].Rows.Count, Is.EqualTo(0));
			var excludes = TestHelper.Fill(String.Format("select * from farm.Excludes where PriceCode = {0} and CatalogId = {1}", priceCode, catalogId));
			Assert.That(excludes.Tables[0].Rows.Count, Is.EqualTo(1));
			// Каталожная запись не проверена, производитель не проверен. Должны вставить в ассортимент
			PrepareTablesCreateAssortmentOrExcludesEntry(priceCode, catalogId, producerId);
			TestHelper.Execute(@"update catalogs.producers set Checked = 0 where Id = {0}", producerId);
			TestHelper.FormalizeOld(typeof(DelimiterNativeTextParser1251), rules, file, priceItemId);
/*			
 * новые записи в ассортименте не создаются
 * assortment = TestHelper.Fill(String.Format("select * from catalogs.assortment where CatalogId = {0} and ProducerId = {1}", catalogId, producerId));
			Assert.That(assortment.Tables[0].Rows.Count, Is.EqualTo(1));*/
			excludes = TestHelper.Fill(String.Format("select * from farm.Excludes where PriceCode = {0} and CatalogId = {1}", priceCode, catalogId));
			Assert.That(excludes.Tables[0].Rows.Count, Is.EqualTo(0));

			// Каталожная запись проверена, производитель не проверен. Должны вставить в исключения
			PrepareTablesCreateAssortmentOrExcludesEntry(priceCode, catalogId, producerId);
			TestHelper.Execute(@"
delete from catalogs.assortment where id = {1} or (CatalogId = {0} and ProducerId = {1});
insert into catalogs.assortment values({1}, {0}, {1}, 1)",
				catalogId, notCheckedProducerId);
			TestHelper.FormalizeOld(typeof(DelimiterNativeTextParser1251), rules, file, priceItemId);
/* новые записи в ассортименте не создаются
 * assortment = TestHelper.Fill(String.Format("select * from catalogs.assortment where CatalogId = {0} and ProducerId = {1}", catalogId, producerId));
			Assert.That(assortment.Tables[0].Rows.Count, Is.EqualTo(0));*/
			excludes = TestHelper.Fill(String.Format("select * from farm.Excludes where PriceCode = {0} and CatalogId = {1}", priceCode, catalogId));
			Assert.That(excludes.Tables[0].Rows.Count, Is.EqualTo(1));
		}
	}
}