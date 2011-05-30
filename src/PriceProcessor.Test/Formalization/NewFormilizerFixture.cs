using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;
using Inforoom.PriceProcessor;
using NUnit.Framework;
using Test.Support;
using Test.Support.Catalog;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Formalization
{
	[TestFixture]
	public class NewFormilizerFixture
	{
		private BasePriceParser2 formalizer;
		private string file;

		private TestPrice price;
		private TestPriceItem priceItem;
		private DataTable table;

		[SetUp]
		public void Setup()
		{
			file = "test.txt";
			using(var scope = new TransactionScope(OnDispose.Rollback))
			{
				price = TestSupplier.CreateTestSupplierWithPrice(p => {
					var rules = p.Costs.Single().PriceItem.Format;
					rules.PriceFormat = PriceFormatType.NativeDelimiter1251;
					rules.Delimiter = ";";
					rules.FName1 = "F1";
					rules.FFirmCr = "F2";
					rules.FQuantity = "F3";
					p.Costs.Single().FormRule.FieldName = "F4";
				});
				priceItem = price.Costs.First().PriceItem;
				scope.VoteCommit();
			}
			table = TestHelper.LoadFormRules(priceItem.Id);
			Settings.Default.SyncPriceCodes.Add(price.Id.ToString());
		}

		[Test]
		public void Do_not_insert_empty_or_zero_costs()
		{
			using(new TransactionScope())
			{
				price.NewPriceCost(priceItem, "F5");
				price.NewPriceCost(priceItem, "F6");
				price.Update();
			}

			File.WriteAllText(file, @"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;24;0;;73.88;", Encoding.GetEncoding(1251));

			using (new TransactionScope())
			{
				var product = TestProduct.Queryable.First();
				var producer = TestProducer.Queryable.First();
				new TestProductSynonym("5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г", product, price).Save();
				new TestProducerSynonym("Санкт-Петербургская ф.ф.", producer, price).Save();
			}

			Formalize();

			using(new SessionScope())
			{
				var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
				Assert.That(cores.Count, Is.EqualTo(1));
				Assert.That(cores.Single().Costs.Select(c => c.Cost).ToList(), Is.EqualTo(new[] {73.88}));
			}
		}

		[Test]
		public void Do_not_create_producer_synonym_if_most_price_unknown()
		{
			var priceContent =  @"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;24;73.88;";
			File.WriteAllText(file, priceContent, Encoding.GetEncoding(1251));

			using (new TransactionScope())
			{
				var product = TestProduct.Queryable.First();
				new TestProductSynonym("5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г", product, price).Save();
			}

			Formalize();

			using(new SessionScope())
			{
				var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
				Assert.That(cores.Select(c => c.ProductSynonym.Name).ToArray(), Is.EqualTo(new [] { "5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г" }));
				Assert.That(cores.Single().ProducerSynonym, Is.Null);
				var synonyms = TestProducerSynonym.Queryable.Where(s => s.Price == price).ToList();
				Assert.That(synonyms, Is.Empty);
			}
		}

		[Test]
		public void Build_new_producer_synonym_if_not_exists()
		{
			using (new TransactionScope())
			{
				var product = TestProduct.Queryable.First();
				new TestProductSynonym("911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ", product, price).Save();

				price.CreateAssortmentBoundSynonyms(
					"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г",
					"Санкт-Петербургская ф.ф.");

				price.CreateAssortmentBoundSynonyms(
					"9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ",
					"Валента Фармацевтика/Королев Ф");
			}

			Formalize(@"9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ;Валента Фармацевтика/Королев Ф;2864;220.92;
5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;24;73.88;
911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ;Твинс Тэк;40;44.71;");

			using(new SessionScope())
			{
				var synonyms = TestProducerSynonym.Queryable.Where(s => s.Price == price).ToList();
				Assert.That(synonyms.Select(s => s.Name).ToArray(), Is.EquivalentTo(new[] {"Санкт-Петербургская ф.ф.", "Твинс Тэк", "Валента Фармацевтика/Королев Ф"}));
				var createdSynonym = synonyms.Single(s => s.Name == "Твинс Тэк");
				Assert.That(createdSynonym.Producer, Is.Null);
				var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
				var core = cores.Single(c => c.ProductSynonym.Name == "911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ");
				Assert.That(core.ProducerSynonym, Is.EqualTo(createdSynonym), "создали синоним но не назначили его позиции в core");

			}
		}

		[Test]
		public void Update_quantity_if_changed()
		{
			Price(@"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;24;73.88;");

			using (new TransactionScope())
			{
				var product = TestProduct.Queryable.First();
				var producer = TestProducer.Queryable.First();
				new TestProductSynonym("5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г", product, price).Save();
				new TestProducerSynonym("Санкт-Петербургская ф.ф.", producer, price).Save();
			}

			Formalize();

			TestCore core;
			using (new SessionScope())
			{
				var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
				core = cores.Single();
				Assert.That(core.Quantity, Is.EqualTo("24"));
			}

			Price(@"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;25;73.88;");

			Formalize();

			using(new SessionScope())
			{
				core.Refresh();
				Assert.That(core.Quantity, Is.EqualTo("25"));
			}
		}

		[Test]
		public void Update_cost_if_changed()
		{
			Price(@"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;25;71.88;");

			using (new TransactionScope())
			{
				var product = TestProduct.Queryable.First();
				var producer = TestProducer.Queryable.First();
				new TestProductSynonym("5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г", product, price).Save();
				new TestProducerSynonym("Санкт-Петербургская ф.ф.", producer, price).Save();
			}

			Formalize();

			TestCore core;
			using (new SessionScope())
			{
				var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
				core = cores.Single();
				Assert.That(core.Costs.Single().Cost, Is.EqualTo(71.88d));
			}

			Price(@"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;25;73.88;");

			Formalize();

			using(new SessionScope())
			{
				core.Refresh();
				Assert.That(core.Costs.Single().Cost, Is.EqualTo(73.88d));
			}
		}

		[Test]
		public void Update_multy_cost()
		{
			Price(@"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;25;71.88;71.56;;");

			using (new TransactionScope())
			{
				price.NewPriceCost(priceItem, "F5");
				price.NewPriceCost(priceItem, "F6");
				price.Update();
				var product = TestProduct.Queryable.First();
				var producer = TestProducer.Queryable.First();
				new TestProductSynonym("5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г", product, price).Save();
				new TestProducerSynonym("Санкт-Петербургская ф.ф.", producer, price).Save();
			}

			Formalize();

			TestCore core;
			using (new SessionScope())
			{
				var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
				core = cores.Single();
				Assert.That(core.Costs.Count, Is.EqualTo(2));
				Assert.That(core.Costs.ElementAt(0).Cost, Is.EqualTo(71.88f));
				Assert.That(core.Costs.ElementAt(0).PriceCost.Id, Is.EqualTo(price.Costs.ElementAt(0).Id));
				Assert.That(core.Costs.ElementAt(1).Cost, Is.EqualTo(71.56f));
				Assert.That(core.Costs.ElementAt(1).PriceCost.Id, Is.EqualTo(price.Costs.ElementAt(1).Id));
			}

			Price(@"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;25;72.10;;73.66;");

			Formalize();

			using (new SessionScope())
			{
				core.Refresh();
				Assert.That(core.Costs.Count, Is.EqualTo(2));
				Assert.That(core.Costs.ElementAt(0).Cost, Is.EqualTo(72.10f));
				Assert.That(core.Costs.ElementAt(0).PriceCost.Id, Is.EqualTo(price.Costs.ElementAt(0).Id));
				Assert.That(core.Costs.ElementAt(1).Cost, Is.EqualTo(73.66f));
				Assert.That(core.Costs.ElementAt(1).PriceCost.Id, Is.EqualTo(price.Costs.ElementAt(2).Id));
			}
		}

		[Test]
		public void Create_forbidden_expressions()
		{
			using(new TransactionScope())
			{
				var product = TestProduct.Queryable.First();
				new TestProductSynonym("5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г", product, price).Save();
				new TestForbidden {Name = "911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ", Price = price}.Save();
			}

			Formalize(@"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;24;73.88;
911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ;Твинс Тэк;40;44.71;
911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ;Твинс Тэк;30;44.71;");

			using (new SessionScope())
			{
				var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
				Assert.That(cores.Count, Is.EqualTo(1));
				Assert.That(cores[0].ProductSynonym.Name, Is.EqualTo("5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г"));
			}
		}

		[Test]
		public void Select_producer_based_on_assortment()
		{
			TestProducer producer1;
			TestProducer producer2;
			TestProduct product1;
			TestProduct product2;

			using (new TransactionScope())
			{
				var producers = TestProducer.Queryable.Take(2).ToList();
				producer1 = producers[0];
				producer2 = producers[1];
				new TestProducerSynonym("Вектор", producer1, price).Save();
				new TestProducerSynonym("Вектор", producer2, price).Save();

				var products = TestProduct.Queryable.Take(2).ToList();
				product1 = products[0];
				product2 = products[1];
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
			}

			Formalize(@"9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ;Валента Фармацевтика/Королев Ф;2864;220.92;
5-нок 50мг Таб. П/о Х50;Вектор;440;66.15;
Теотард 200мг Капс.пролонг.дейст. Х40;Вектор;157;83.02;");

			using (new SessionScope())
			{
				var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
				Assert.That(cores.Count, Is.EqualTo(3));
				var core1 = cores[1];
				Assert.That(core1.Product.Id, Is.EqualTo(product1.Id));
				Assert.That(core1.Producer.Id, Is.EqualTo(producer1.Id));

				var core2 = cores[2];
				Assert.That(core2.Product.Id, Is.EqualTo(product2.Id));
				Assert.That(core2.Producer.Id, Is.EqualTo(producer2.Id));
			}
		}

		[Test]
		public void Create_new_automatic_synonym_if_do_not_have_excludes()
		{
			TestProducer producer1;
			TestProducer producer2;
			TestProduct product1;
			TestProduct product2;
			TestProducerSynonym synonym1;
			TestProducerSynonym synonym2;

			using (new TransactionScope())
			{
				var producers = TestProducer.Queryable.Take(2).ToList();
				producer1 = producers[0];
				producer2 = producers[1];
				synonym1 = new TestProducerSynonym("Вектор", producer1, price);
				synonym1.Save();
				synonym2 = new TestProducerSynonym("Вектор", producer2, price);
				synonym2.Save();

				var products = TestProduct.Queryable.Take(2).ToList();
				product1 = products[0];
				product2 = products[1];
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
			}

			Formalize(@"9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ;Валента Фармацевтика/Королев Ф;2864;220.92;
5-нок 50мг Таб. П/о Х50;Вектор;440;66.15;
Теотард 200мг Капс.пролонг.дейст. Х40;Вектор;157;83.02;");

			using (new SessionScope())
			{
				var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
				Assert.That(cores.Count, Is.EqualTo(3));
				var core1 = cores[1];
				Assert.That(core1.Product.Id, Is.EqualTo(product1.Id));
				Assert.That(core1.ProducerSynonym.Id, Is.Not.EqualTo(synonym1.Id).And.Not.EqualTo(synonym2.Id));
				Assert.That(core1.ProducerSynonym.Name, Is.EqualTo("Вектор"));
				Assert.That(core1.Producer, Is.Null);

				var core2 = cores[2];
				Assert.That(core2.Product.Id, Is.EqualTo(product2.Id));
				Assert.That(core2.ProducerSynonym.Id, Is.Not.EqualTo(synonym1.Id).And.Not.EqualTo(synonym2.Id));
				Assert.That(core2.ProducerSynonym.Name, Is.EqualTo("Вектор"));
				Assert.That(core2.Producer, Is.Null);
				var excludes = TestExclude.Queryable.Where(c => c.Price == price).ToList();
				Assert.That(excludes.Count, Is.EqualTo(0));
			}
		}

		[Test]
		public void Create_exclude_if_synonym_without_producer_exist()
		{
			TestProduct product1;
			using (new TransactionScope())
			{
				new TestProducerSynonym("Вектор", null, price).Save();
				product1 = TestProduct.Queryable.First();
				new TestProductSynonym("5-нок 50мг Таб. П/о Х50", product1, price).Save();
			}

			Formalize(@"5-нок 50мг Таб. П/о Х50;Вектор;440;66.15;");

			using (new SessionScope())
			{
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
		}

		[Test]
		public void Create_assortment_if_product_not_pharmacie()
		{
			using (new TransactionScope())
			{
				price.AddProducerSynonym("Вектор", new TestProducer("KRKA"));
				price.AddProductSynonym("5-нок 50мг Таб. П/о Х50", new TestProduct("5-нок 50мг Таб. П/о Х50"));
				price.Save();
			}

			Formalize(@"5-нок 50мг Таб. П/о Х50;Вектор;440;66.15;");

			using (new SessionScope())
			{
				price.Refresh();
				var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
				Assert.That(price.ProductSynonyms[0].Product.CatalogProduct.Producers,
					Is.EquivalentTo(new [] {price.ProducerSynonyms[0].Producer }));
			}
		}

		[Test]
		public void Prefer_producer_synonym_with_producer()
		{
			using (new TransactionScope())
			{
				price.AddProducerSynonym("Вектор", null);
				price.AddProducerSynonym("Вектор", new TestProducer("KRKA"));
				price.AddProductSynonym("5-нок 50мг Таб. П/о Х50", new TestProduct("5-нок 50мг Таб. П/о Х50"));
				price.Save();
			}

			Formalize(@"5-нок 50мг Таб. П/о Х50;Вектор;440;66.15;");

			using (new SessionScope())
			{
				price = TestPrice.Find(price.Id);
				Assert.That(price.Core[0].Producer, Is.Not.Null);
			}
		}

		private void Price(string contents)
		{
			File.WriteAllText(file, contents, Encoding.GetEncoding(1251));
		}

		private void Formalize(string content)
		{
			Price(content);
			Formalize();
		}

		private void Formalize()
		{
			var row = table.Rows[0];
			var reader = new PriceReader(row, new TextParser(new DelimiterSlicer(";"), Encoding.GetEncoding(1251), -1), file, new PriceFormalizationInfo(row));
			formalizer = new BasePriceParser2(reader, row);
			formalizer.Formalize();
		}
	}
}
