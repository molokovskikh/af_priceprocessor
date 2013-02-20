﻿using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.MySql;
using Common.Tools;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Models;
using MySql.Data.MySqlClient;
using NHibernate.Linq;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using Test.Support.Catalog;

namespace PriceProcessor.Test.Formalization
{
	[TestFixture]
	public class NewFormilizerFixture : BaseFormalizationFixture
	{
		[SetUp]
		public void Setup()
		{
			CreatePrice();
			Settings.Default.SyncPriceCodes.Add(price.Id.ToString());
		}

		[Test]
		public void Do_not_insert_empty_or_zero_costs()
		{
			price.NewPriceCost(priceItem, "F5");
			price.NewPriceCost(priceItem, "F6");
			var producer = TestProducer.Queryable.First();
			price.AddProductSynonym("5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г");
			new TestProducerSynonym("Санкт-Петербургская ф.ф.", producer, price).Save();
			price.Update();

			Formalize(@"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;24;0;;73.88;");

			var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
			Assert.That(cores.Count, Is.EqualTo(1));
			Assert.That(cores.Single().Costs.Select(c => c.Cost).ToList(), Is.EqualTo(new[] { 73.88 }));
		}

		[Test]
		public void Do_not_create_producer_synonym_if_most_price_unknown()
		{
			price.AddProductSynonym("5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г");
			price.Update();

			Formalize(@"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;24;73.88;");

			var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
			Assert.That(cores.Select(c => c.ProductSynonym.Name).ToArray(), Is.EqualTo(new[] { "5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г" }));
			Assert.That(cores.Single().ProducerSynonym, Is.Null);
			var synonyms = TestProducerSynonym.Queryable.Where(s => s.Price == price).ToList();
			Assert.That(synonyms, Is.Empty);
		}

		[Test]
		public void Build_new_producer_synonym_if_not_exists()
		{
			price.AddProductSynonym("911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ");
			price.CreateAssortmentBoundSynonyms(
				"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г",
				"Санкт-Петербургская ф.ф.");
			price.CreateAssortmentBoundSynonyms(
				"9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ",
				"Валента Фармацевтика/Королев Ф");
			price.Update();

			Formalize(@"9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ;Валента Фармацевтика/Королев Ф;2864;220.92;
5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;24;73.88;
911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ;Твинс Тэк;40;44.71;");

			var synonyms = TestProducerSynonym.Queryable.Where(s => s.Price == price).ToList();
			Assert.That(synonyms.Select(s => s.Name).ToArray(), Is.EquivalentTo(new[] { "Санкт-Петербургская ф.ф.", "Твинс Тэк", "Валента Фармацевтика/Королев Ф" }));
			var createdSynonym = synonyms.Single(s => s.Name == "Твинс Тэк");
			Assert.That(createdSynonym.Producer, Is.Null);
			var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
			var core = cores.Single(c => c.ProductSynonym.Name == "911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ");
			Assert.That(core.ProducerSynonym, Is.EqualTo(createdSynonym), "создали синоним но не назначили его позиции в core");
		}

		[Test, Description("Проверяем, что при формализации прайса мы не создаем автоматический синоним, созданный по ассортименту")]
		public void Complex_double_firmalize_no_automatic_synonim()
		{
			With.Connection(c => {
				var deleter = c.CreateCommand();
				deleter.CommandText = "delete from AutomaticProducerSynonyms";
				deleter.ExecuteNonQuery();
			});

			var product = new TestProduct("9 МЕСЯЦЕВ КРЕМ ДЛЯ ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ");
			product.CatalogProduct.Pharmacie = true;
			product.CatalogProduct.Monobrend = true;
			session.Save(product);
			var producer = new TestProducer("Валента Фармацевтика/Королев Ф");
			session.Save(producer);
			price.AddProductSynonym("9 МЕСЯЦЕВ КРЕМ ДЛЯ ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ", product);
			price.CreateAssortmentBoundSynonyms(
				"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г",
				"Санкт-Петербургская ф.ф.");
			session.Save(price);
			TestAssortment.CheckAndCreate(product, producer);
			Close();

			Price(@"9 МЕСЯЦЕВ КРЕМ ДЛЯ ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ;Валента Фармацевтика/Королев Ф;2864;220.92;
5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;24;73.88;");

			Formalize();
			Formalize();

			With.Connection(c => {
				var counter = c.CreateCommand();
				counter.CommandText = "select count(*) from AutomaticProducerSynonyms";
				var count = counter.ExecuteScalar();
				Assert.That(count, Is.EqualTo(0));
			});
		}

		[Test]
		public void Update_quantity_if_changed()
		{
			Price(@"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;24;73.88;");

			var producer = TestProducer.Queryable.First();
			price.AddProductSynonym("5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г");
			new TestProducerSynonym("Санкт-Петербургская ф.ф.", producer, price).Save();
			price.Update();

			Formalize();

			var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
			var core = cores.Single();
			Assert.That(core.Quantity, Is.EqualTo("24"));

			Price(@"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;25;73.88;");

			Formalize();

			session.Refresh(core);
			Assert.That(core.Quantity, Is.EqualTo("25"));
		}

		[Test]
		public void Update_cost_if_changed()
		{
			Price(@"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;25;71.88;");

			var producer = TestProducer.Queryable.First();
			new TestProducerSynonym("Санкт-Петербургская ф.ф.", producer, price).Save();
			price.AddProductSynonym("5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г");
			price.Update();

			Formalize();

			var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
			var core = cores.Single();
			Assert.That(core.Costs.Single().Cost, Is.EqualTo(71.88d));

			Price(@"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;25;73.88;");

			Formalize();

			core.Refresh();
			Assert.That(core.Costs.Single().Cost, Is.EqualTo(73.88d));
		}

		[Test]
		public void Update_multy_cost()
		{
			Price(@"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;25;71.88;71.56;;");

			price.NewPriceCost(priceItem, "F5");
			price.NewPriceCost(priceItem, "F6");
			price.Update();
			price.AddProductSynonym("5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г");
			var producer = TestProducer.Queryable.First();
			new TestProducerSynonym("Санкт-Петербургская ф.ф.", producer, price).Save();

			Formalize();

			var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
			var core = cores.Single();
			Assert.That(core.Costs.Count, Is.EqualTo(2));
			Assert.That(core.Costs.ElementAt(0).Cost, Is.EqualTo(71.88f));
			Assert.That(core.Costs.ElementAt(0).Id.CostId, Is.EqualTo(price.Costs.ElementAt(0).Id));
			Assert.That(core.Costs.ElementAt(1).Cost, Is.EqualTo(71.56f));
			Assert.That(core.Costs.ElementAt(1).Id.CostId, Is.EqualTo(price.Costs.ElementAt(1).Id));

			Price(@"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;25;72.10;;73.66;");

			Formalize();

			session.Refresh(core);
			Assert.That(core.Costs.Count, Is.EqualTo(2));
			Assert.That(core.Costs.ElementAt(0).Cost, Is.EqualTo(72.10f));
			Assert.That(core.Costs.ElementAt(0).Id.CostId, Is.EqualTo(price.Costs.ElementAt(0).Id));
			Assert.That(core.Costs.ElementAt(1).Cost, Is.EqualTo(73.66f));
			Assert.That(core.Costs.ElementAt(1).Id.CostId, Is.EqualTo(price.Costs.ElementAt(2).Id));
		}

		[Test]
		public void Create_forbidden_expressions()
		{
			price.AddProductSynonym("5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г");
			new TestForbidden { Name = "911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ", Price = price }.Save();
			price.Update();

			Formalize(@"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;24;73.88;
911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ;Твинс Тэк;40;44.71;
911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ;Твинс Тэк;30;44.71;");

			var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
			Assert.That(cores.Count, Is.EqualTo(1));
			Assert.That(cores[0].ProductSynonym.Name, Is.EqualTo("5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г"));
		}

		[Test]
		public void Select_producer_based_on_assortment()
		{
			var producers = TestProducer.Queryable.Take(2).ToList();
			var producer1 = producers[0];
			var producer2 = producers[1];
			new TestProducerSynonym("Вектор", producer1, price).Save();
			new TestProducerSynonym("Вектор", producer2, price).Save();

			var products = TestProduct.Queryable.Take(2).ToList();
			var product1 = products[0];
			var product2 = products[1];
			new TestProductSynonym("5-нок 50мг Таб. П/о Х50", product1, price).Save();
			new TestProductSynonym("Теотард 200мг Капс.пролонг.дейст. Х40", product2, price).Save();
			price.CreateAssortmentBoundSynonyms(
				"9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ",
				"Валента Фармацевтика/Королев Ф");

			var brokenAssortment = TestAssortment.Queryable.FirstOrDefault(a => a.Producer == producer1 && a.Catalog == product2.CatalogProduct);
			if (brokenAssortment != null)
				brokenAssortment.Delete();

			brokenAssortment = TestAssortment.Queryable.FirstOrDefault(a => a.Producer == producer2 && a.Catalog == product1.CatalogProduct);
			if (brokenAssortment != null)
				brokenAssortment.Delete();

			var assortment1 = TestAssortment.Queryable.FirstOrDefault(a => a.Producer == producer1 && a.Catalog == product1.CatalogProduct);
			if (assortment1 == null)
				new TestAssortment(product1, producer1).Save();

			var assortment2 = TestAssortment.Queryable.FirstOrDefault(a => a.Producer == producer2 && a.Catalog == product2.CatalogProduct);
			if (assortment2 == null)
				new TestAssortment(product2, producer2).Save();
			price.Save();

			Formalize(@"9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ;Валента Фармацевтика/Королев Ф;2864;220.92;
5-нок 50мг Таб. П/о Х50;Вектор;440;66.15;
Теотард 200мг Капс.пролонг.дейст. Х40;Вектор;157;83.02;");

			var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
			Assert.That(cores.Count, Is.EqualTo(3));
			var core1 = cores[1];
			Assert.That(core1.Product.Id, Is.EqualTo(product1.Id));
			Assert.That(core1.Producer.Id, Is.EqualTo(producer1.Id));

			var core2 = cores[2];
			Assert.That(core2.Product.Id, Is.EqualTo(product2.Id));
			Assert.That(core2.Producer.Id, Is.EqualTo(producer2.Id));
		}

		[Test]
		public void Create_new_automatic_synonym_if_do_not_have_excludes()
		{
			var producer1 = new TestProducer("Тестовый производитель1");
			var producer2 = new TestProducer("Тестовый производитель2");
			var synonym1 = new TestProducerSynonym("Вектор", producer1, price);
			synonym1.Save();
			var synonym2 = new TestProducerSynonym("Вектор", producer2, price);
			synonym2.Save();

			var products = TestProduct.Queryable.Take(2).ToList();
			var product1 = products[0];
			product1.CatalogProduct.Pharmacie = true;
			product1.Save();
			var product2 = products[1];
			product2.CatalogProduct.Pharmacie = true;
			product2.Save();
			new TestProductSynonym("5-нок 50мг Таб. П/о Х50", product1, price).Save();
			new TestProductSynonym("Теотард 200мг Капс.пролонг.дейст. Х40", product2, price).Save();

			var assortment1 = TestAssortment.Queryable.FirstOrDefault(a => a.Producer == producer1 && a.Catalog == product1.CatalogProduct);
			if (assortment1 != null)
				assortment1.Delete();

			var assortment2 = TestAssortment.Queryable.FirstOrDefault(a => a.Producer == producer2 && a.Catalog == product2.CatalogProduct);
			if (assortment2 != null)
				assortment2.Delete();
			price.CreateAssortmentBoundSynonyms(
				"9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ",
				"Валента Фармацевтика/Королев Ф");
			price.Save();

			Formalize(@"9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ;Валента Фармацевтика/Королев Ф;2864;220.92;
5-нок 50мг Таб. П/о Х50;Вектор;440;66.15;
Теотард 200мг Капс.пролонг.дейст. Х40;Вектор;157;83.02;");

			var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
			Assert.That(cores.Count, Is.EqualTo(3));
			var core1 = cores.Single(c => c.ProductSynonym.Name == "5-нок 50мг Таб. П/о Х50");
			Assert.That(core1.Product.Id, Is.EqualTo(product1.Id));
			Assert.That(core1.ProducerSynonym.Id, Is.Not.EqualTo(synonym1.Id).And.Not.EqualTo(synonym2.Id));
			Assert.That(core1.ProducerSynonym.Name, Is.EqualTo("Вектор"));
			Assert.That(core1.Producer, Is.Null);

			var core2 = cores.Single(c => c.ProductSynonym.Name == "Теотард 200мг Капс.пролонг.дейст. Х40");
			Assert.That(core2.Product.Id, Is.EqualTo(product2.Id));
			Assert.That(core2.ProducerSynonym.Id, Is.Not.EqualTo(synonym1.Id).And.Not.EqualTo(synonym2.Id));
			Assert.That(core2.ProducerSynonym.Name, Is.EqualTo("Вектор"));
			Assert.That(core2.Producer, Is.Null);
			var excludes = TestExclude.Queryable.Where(c => c.Price == price).ToList();
			Assert.That(excludes.Count, Is.EqualTo(0));
		}

		[Test, Ignore("Все исключения создаются теперь в ручную")]
		public void Create_exclude_if_synonym_without_producer_exist()
		{
			new TestProducerSynonym("Вектор", null, price).Save();
			var product1 = TestProduct.Queryable.First();
			new TestProductSynonym("5-нок 50мг Таб. П/о Х50", product1, price).Save();

			Formalize(@"5-нок 50мг Таб. П/о Х50;Вектор;440;66.15;");

			var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
			Assert.That(cores.Count, Is.EqualTo(1));
			var core1 = cores[0];
			Assert.That(core1.Product.Id, Is.EqualTo(product1.Id));
			Assert.That(core1.ProducerSynonym.Name, Is.EqualTo("Вектор"));
			Assert.That(core1.Producer, Is.Null);

			var excludes = TestExclude.Queryable.Where(c => c.Price == price).ToList();
			Assert.That(excludes.Count, Is.EqualTo(1));
			var exclude = excludes[0];
			Assert.That(exclude.ProducerSynonym, Is.EqualTo("Вектор"));
			Assert.That(exclude.CatalogProduct.Id, Is.EqualTo(product1.CatalogProduct.Id));
		}

		[Test, Ignore("Больше не создаем ассортимент автоматически, все вручную")]
		public void Create_assortment_if_product_not_pharmacie()
		{
			price.AddProducerSynonym("Вектор", new TestProducer("KRKA"));
			price.AddProductSynonym("5-нок 50мг Таб. П/о Х50", new TestProduct("5-нок 50мг Таб. П/о Х50"));
			price.Save();

			Formalize(@"5-нок 50мг Таб. П/о Х50;Вектор;440;66.15;");

			price.Refresh();
			var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
			Assert.That(price.ProductSynonyms[0].Product.CatalogProduct.Producers,
				Is.EquivalentTo(new[] { price.ProducerSynonyms[0].Producer }));
		}

		[Test]
		public void Prefer_producer_synonym_with_producer()
		{
			price.AddProducerSynonym("Вектор", null);
			price.AddProducerSynonym("Вектор", new TestProducer("KRKA"));
			price.AddProductSynonym("5-нок 50мг Таб. П/о Х50", new TestProduct("5-нок 50мг Таб. П/о Х50"));
			price.Save();

			Formalize(@"5-нок 50мг Таб. П/о Х50;Вектор;440;66.15;");

			session.Refresh(price);
			Assert.That(price.Core[0].Producer, Is.Not.Null);
		}

		[Test]
		public void Mark_position_as_junk_if_period_expired()
		{
			var format = price.Costs.First().PriceItem.Format;
			format.FPeriod = "F5";
			format.Update();

			price.AddProductSynonym("5-нок 50мг Таб. П/о Х50");
			price.Update();

			var bestUseFor = DateTime.Now.AddDays(60).ToShortDateString();
			Formalize(String.Format(@"5-нок 50мг Таб. П/о Х50;Вектор;440;66.15;{0}", bestUseFor));

			session.Refresh(price);
			var core = price.Core.First();
			Assert.That(core.Period, Is.EqualTo(bestUseFor));
			Assert.That(core.Junk, Is.True);
		}

		[Test]
		public void Create_synonym_with_same_name()
		{
			//что бы получить статистику и обойти проверку на вставку синонимов производителя
			price.AddProducerSynonym("Вектор", new TestProducer("KRKA"));
			price.AddProductSynonym("5-нок 50мг Таб. П/о Х50", new TestProduct("5-нок 50мг Таб. П/о Х50"));

			var producer1 = session.Query<TestProducer>().First();
			var producer2 = session.Query<TestProducer>().Skip(1).First();
			var product1 = new TestProduct("Финалгон мазь 20г");
			product1.CatalogProduct.Pharmacie = true;
			product1.CatalogProduct.Monobrend = true;
			session.Save(product1);
			session.Save(new TestAssortment(product1, producer1));

			var product2 = new TestProduct("Актовегин таб 200мг №10");
			product2.CatalogProduct.Pharmacie = true;
			session.Save(product2);
			session.Save(new TestAssortment(product2, producer2));

			price.AddProductSynonym("Финалгон мазь 20г", product1);
			price.AddProductSynonym("Актовегин таб 200мг №10", product2);
			session.Save(price);
			session.Flush();

			Formalize(@"Финалгон мазь 20г;Глобофарм фармацойтише Продуктьонс унд Х;40;192.67;
Актовегин таб 200мг №10;Глобофарм фармацойтише Продуктьонс унд Х;40;521.79;
5-нок 50мг Таб. П/о Х50;Вектор;440;66.15;
Теотард 200мг Капс.пролонг.дейст. Х40;Вектор;157;83.02;");

			session.Refresh(price);
			var synonyms = price.ProducerSynonyms.Where(s => s.Name == "Глобофарм фармацойтише Продуктьонс унд Х")
				.ToArray();
			Assert.That(synonyms.Length, Is.EqualTo(2), "price id = {0}", price.Id);
			Assert.That(synonyms.Count(s => s.Producer == null), Is.EqualTo(1), synonyms.Implode());
			Assert.That(synonyms.Count(s => s.Producer != null && s.Producer.Id == producer1.Id),
				Is.EqualTo(1),
				synonyms.Implode());

			var unrecExceptions = session.Query<TestUnrecExp>().Where(e => e.PriceItemId == priceItem.Id).ToList();
			Assert.That(unrecExceptions.Count, Is.EqualTo(2));
		}

		[Test]
		public void Respect_is_automatic_flag()
		{
			//проверяем что IsAutomatic true
			//мы создадим запись в AutomaticProducerSynonyms
			//а если IsAutomatic false
			//то не создаем
			FakeParserSynonymTest(false, 0, typeof(FakeParser));
			FakeParserSynonymTest(false, 0, typeof(FakeParser2));
			FakeParserSynonymTest(true, 1, typeof(FakeParser));
			FakeParserSynonymTest(true, 1, typeof(FakeParser2));
		}

		[Test]
		public void Fill_exp_field()
		{
			priceItem.Format.FPeriod = "F5";
			CreateDefaultSynonym();

			Formalize(@"9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ;Валента Фармацевтика/Королев Ф;2864;220.92;10.12.2014;
5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;24;73.88;янв;
911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ;Твинс Тэк;40;44.71;");

			session.Refresh(price);
			var core = price.Core.First(c => c.ProductSynonym.Name.StartsWith("9 МЕСЯЦЕВ"));
			Assert.That(core.Exp, Is.EqualTo(new DateTime(2014, 12, 10)));
			core = price.Core.First(c => c.ProductSynonym.Name.StartsWith("5 ДНЕЙ"));
			Assert.That(core.Exp, Is.Null);
		}

		private void FillDaSynonymFirmCr2(FakeParser2 parser, MySqlConnection connection, bool automatic)
		{
			Clean(connection);
			parser.Prepare();
			parser.DaSynonymFirmCr.InsertCommand.Parameters["?PriceCode"].Value = price.Id;
			parser.DaSynonymFirmCr.InsertCommand.Parameters["?OriginalSynonym"].Value = "123";
			parser.DaSynonymFirmCr.InsertCommand.Parameters["?IsAutomatic"].Value = automatic;
			parser.DaSynonymFirmCr.InsertCommand.ExecuteNonQuery();
		}

		private void FillDaSynonymFirmCr(FakeParser parser, MySqlConnection connection, bool automatic)
		{
			Clean(connection);
			parser.Prepare();
			parser.DaSynonymFirmCr.InsertCommand.Parameters["?PriceCode"].Value = price.Id;
			parser.DaSynonymFirmCr.InsertCommand.Parameters["?OriginalSynonym"].Value = "456";
			parser.DaSynonymFirmCr.InsertCommand.Parameters["?CodeFirmCr"].Value = null;
			parser.DaSynonymFirmCr.InsertCommand.Parameters["?IsAutomatic"].Value = automatic;
			parser.DaSynonymFirmCr.InsertCommand.ExecuteNonQuery();
		}

		private void Clean(MySqlConnection connection)
		{
			var deleter = connection.CreateCommand();
			deleter.CommandText = "delete from AutomaticProducerSynonyms;" +
				"delete from Farm.SynonymFirmCr where PriceCode = ?priceId";
			deleter.Parameters.AddWithValue("priceId", price.Id);
			deleter.ExecuteNonQuery();
		}

		private void FakeParserSynonymTest(bool automatic, int automaticProducerSynonyms, Type fakeType)
		{
			if (session.Transaction.IsActive)
				session.Transaction.Commit();

			var table = PricesValidator.LoadFormRules(priceItem.Id);
			var row = table.Rows[0];
			var info = new PriceFormalizationInfo(row, null);
			if (fakeType == typeof(FakeParser)) {
				var parser = new FakeParser(null, (MySqlConnection)session.Connection, info);
				FillDaSynonymFirmCr(parser, (MySqlConnection)session.Connection, automatic);
			}
			else {
				var parser = new FakeParser2(new FakeReader(), info);
				if (parser.Connection.State != ConnectionState.Open)
					parser.Connection.Open();
				FillDaSynonymFirmCr2(parser, (MySqlConnection)session.Connection, automatic);
				parser.Connection.Close();
			}
			var counter = session.Connection.CreateCommand();
			counter.CommandText = "select count(*) from AutomaticProducerSynonyms";
			Assert.That(Convert.ToInt32(counter.ExecuteScalar()), Is.EqualTo(automaticProducerSynonyms));
		}
	}
}