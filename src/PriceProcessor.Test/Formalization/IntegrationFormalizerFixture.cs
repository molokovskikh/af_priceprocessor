using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;
using Inforoom.PriceProcessor.Models;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Formalization
{
	public class IntegrationFormalizerFixture : IntegrationFixture
	{
		private BasePriceParser2 formalizer;
		private string file;

		private TestPrice price;
		private TestPriceItem priceItem;

		[SetUp]
		public void Setup()
		{
			file = "test.txt";
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
			session.Save(price);
			session.Flush();
			Settings.Default.SyncPriceCodes.Add(price.Id.ToString());
		}

		[Test]
		public void FormalizePositionWithForbiddenProducer()
		{
			var product = new TestProduct("9 МЕСЯЦЕВ КРЕМ ДЛЯ ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ");
			product.CatalogProduct.Pharmacie = true;
			session.Save(product);
			var query = session.CreateSQLQuery("delete from farm.unrecexp");
			query.UniqueResult();
			var forbiddenProducer = new ForbiddenProducerNames {
				Name = "Валента Фармацевтика/Королев Ф"
			};
			session.Save(forbiddenProducer);
			session.Flush();
			var producer = new TestProducer("Валента Фармацевтика/Королев Ф");
			session.Save(producer);
			var newPrice = session.Load<Price>(price.Id);
			var synonym = new ProductSynonym {
				Price = newPrice,
				Product = session.Load<Product>(product.Id),
				Synonym = "9 МЕСЯЦЕВ КРЕМ ДЛЯ ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ"
			};
			session.Save(synonym);
			Reopen();
			Price(@"9 МЕСЯЦЕВ КРЕМ ДЛЯ ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ;Валента Фармацевтика/Королев Ф;2864;220.92;");
			Formalize();
			query = session.CreateSQLQuery("select count(*) from farm.unrecexp");
			var result = Convert.ToInt32(query.UniqueResult());

			Assert.That(result == 0);
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
			var table = PricesValidator.LoadFormRules(priceItem.Id);
			var row = table.Rows[0];
			var info = new PriceFormalizationInfo(row, null);
			var reader = new PriceReader(row, new TextParser(new DelimiterSlicer(";"), Encoding.GetEncoding(1251), -1), file, info);
			formalizer = new BasePriceParser2(reader, info);
			formalizer.Formalize();
		}
	}
}
