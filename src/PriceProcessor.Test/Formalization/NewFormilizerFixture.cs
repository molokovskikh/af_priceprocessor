using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;
using Inforoom.PriceProcessor.Properties;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using Test.Support;

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
			using(new SessionScope())
			{
				price = new TestPrice {
					CostType = 0, //мультиколоночный
					FirmCode = 1179 //демонстрационыый поставщик
				};
				price.Save();

				var source = new TestPriceSource {
					SourceType = PriceSourceType.Email,
				};
				source.Save();

				var format = new TestFormat {
					PriceFormat = PriceFormatType.NativeDelimiter1251,
					Delimiter = ";",
					FName1 = "F1",
					FFirmCr = "F2",
					FQuantity = "F3"
				};
				format.Save();

				priceItem = new TestPriceItem {
					Source = source,
					Format = format,
				};
				priceItem.Save();

				price.NewPriceCost(priceItem).FormRule.FieldName = "F4";
				price.Update();
			}
			table = TestHelper.GetParseRules((int) priceItem.Id);
			Settings.Default.SyncPriceCodes.Add(price.Id.ToString());
		}

		[Test]
		public void Do_not_insert_empty_or_zero_costs()
		{
			using(new TransactionScope())
			{
				price.NewPriceCost(priceItem).FormRule.FieldName = "F5";
				price.NewPriceCost(priceItem).FormRule.FieldName = "F6";
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

			Formilize();

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

			Formilize();

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
			Price(
@"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;24;73.88;
911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ;Твинс Тэк;40;44.71;");

			using (new TransactionScope())
			{
				var product = TestProduct.Queryable.First();
				var producer = TestProducer.Queryable.First();
				new TestProducerSynonym("Санкт-Петербургская ф.ф.", producer, price).Save();
				new TestProductSynonym("5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г", product, price).Save();
				new TestProductSynonym("911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ", product, price).Save();
			}

			Formilize();

			using(new SessionScope())
			{
				var synonyms = TestProducerSynonym.Queryable.Where(s => s.Price == price).ToList();
				Assert.That(synonyms.Select(s => s.Name).ToArray(), Is.EqualTo(new[] {"Санкт-Петербургская ф.ф.", "Твинс Тэк"}));
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

			Formilize();

			TestCore core;
			using (new SessionScope())
			{
				var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
				core = cores.Single();
				Assert.That(core.Quantity, Is.EqualTo("24"));
			}

			Price(@"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;25;73.88;");

			Formilize();

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

			Formilize();

			TestCore core;
			using (new SessionScope())
			{
				var cores = TestCore.Queryable.Where(c => c.Price == price).ToList();
				core = cores.Single();
				Assert.That(core.Costs.Single().Cost, Is.EqualTo(71.88d));
			}

			Price(@"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;25;73.88;");

			Formilize();

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
				price.NewPriceCost(priceItem).FormRule.FieldName = "F5";
				price.NewPriceCost(priceItem).FormRule.FieldName = "F6";
				price.Update();
				var product = TestProduct.Queryable.First();
				var producer = TestProducer.Queryable.First();
				new TestProductSynonym("5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г", product, price).Save();
				new TestProducerSynonym("Санкт-Петербургская ф.ф.", producer, price).Save();
			}

			Formilize();

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

			Formilize();

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

		private void Price(string contents)
		{
			File.WriteAllText(file, contents, Encoding.GetEncoding(1251));
		}

		private void Formilize()
		{
			var row = table.Rows[0];
			var reader = new PriceReader(row, new TextParser(new DelimiterSlicer(";"), Encoding.GetEncoding(1251), -1), file, new PriceFormalizationInfo(row));
			formalizer = new BasePriceParser2(reader, row);
			formalizer.Formalize();
		}
	}
}
