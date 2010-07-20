using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Inforoom.Formalizer;
using NUnit.Framework;
using Test.Support;

namespace PriceProcessor.Test.Loader
{
	[TestFixture]
	public class FarmImpeksFormalizerFixture
	{
		private List<TestPrice> prices = new List<TestPrice>();
		private TestPriceItem priceItem;

		[SetUp]
		public void Setup()
		{
			using(new SessionScope())
			{
				priceItem = new TestPriceItem {
					Source = new TestPriceSource {
						SourceType = PriceSourceType.Email,
					},
					Format = new TestFormat {
						PriceFormat = PriceFormatType.FarmImpeks,
						Delimiter = ";",
						FName1 = "F1",
						FFirmCr = "F2",
						FQuantity = "F3"
					},
				};

				var firmCode = TestOldClient.CreateTestSupplier().Id;
				var price = new TestPrice {
					CostType = CostType.MultiColumn, //мультиколоночный
					FirmCode = firmCode, //демонстрационыый поставщик
					ParentSynonym = 4745,
					PriceName = "2"
				};
				price.NewPriceCost(priceItem).FormRule.FieldName = "123";
				price.SaveAndFlush();
				price.Maintain();
				prices.Add(price);

				price = new TestPrice {
					CostType = CostType.MultiColumn, //мультиколоночный
					FirmCode = firmCode, //демонстрационыый поставщик
					ParentSynonym = 4745,
					PriceName = "11"
				};
				price.NewPriceCost(priceItem).FormRule.FieldName = "123";
				price.SaveAndFlush();
				price.Maintain();
				prices.Add(price);
			}
		}

		[Test]
		public void Load_xml_source()
		{
			var formalizer = PricesValidator.Validate(@"..\..\Data\FarmImpecsPrice.xml", Path.GetTempFileName(), priceItem.Id);
			formalizer.Formalize();
			using(new SessionScope())
			foreach (var price in prices)
			{
				Assert.That(TestCore.Queryable.Count(c => c.Price == price), Is.GreaterThan(0), "нет предложений, прайс {0} {1}", price.PriceName, price.Id);
				Assert.That(TestCost.Queryable.Count(c => c.PriceCost == price.Costs.Single()), Is.GreaterThan(0), "нет цен, прайс {0} {1}", price.PriceName, price.Id);
			}
		}

		[Test]
		public void Enable_price_for_client_with_supplier_client_id()
		{
			var trustedClient = TestClient.CreateSimple();
			var normalClient = TestClient.CreateSimple();
			var price = prices[1];
			using(new SessionScope())
			{
				var trustedIntersection = TestIntersection.Queryable.Single(i => i.Price == price && i.Client == trustedClient);
				trustedIntersection.SupplierClientId = "4873";
				trustedIntersection.PriceMarkup = -1;
				trustedIntersection.Save();
			}

			var formalizer = PricesValidator.Validate(@"..\..\Data\FarmImpecsPrice.xml", Path.GetTempFileName(), priceItem.Id);
			formalizer.Formalize();
			using(new SessionScope())
			{
				var regularIntersection = TestIntersection.Queryable.Single(i => i.Price == price && i.Client == normalClient);
				Assert.That(regularIntersection.AvailableForClient, Is.False);

				var trustedIntersection = TestIntersection.Queryable.Single(i => i.Price == price && i.Client == trustedClient);
				Assert.That(trustedIntersection.AvailableForClient, Is.True);
			}
		}
	}
}