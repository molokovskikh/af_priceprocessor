using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Models;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Formalization
{
	public class IntegrationFormalizerFixture : BaseFormalizationFixture
	{
		[SetUp]
		public void Setup()
		{
			CreatePrice();
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
	}
}
