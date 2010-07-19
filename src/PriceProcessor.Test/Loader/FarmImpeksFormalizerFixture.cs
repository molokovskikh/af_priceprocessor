using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Castle.ActiveRecord;
using Common.Tools;
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
				TestPrice.Queryable.Where(p => p.FirmCode == 1179u).ToList().Each(p => p.Delete());
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

				var price = new TestPrice {
					CostType = CostType.MultiColumn, //мультиколоночный
					FirmCode = 1179, //демонстрационыый поставщик
					ParentSynonym = 4745,
					PriceName = "2"
				};
				price.NewPriceCost(priceItem).FormRule.FieldName = "123";
				price.Save();
				prices.Add(price);

				price = new TestPrice {
					CostType = CostType.MultiColumn, //мультиколоночный
					FirmCode = 1179, //демонстрационыый поставщик
					ParentSynonym = 4745,
					PriceName = "3"
				};
				price.NewPriceCost(priceItem).FormRule.FieldName = "123";
				price.Save();
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
				Assert.That(TestCore.Queryable.Count(c => c.Price == price), Is.GreaterThan(0), "нет предложений");
				Assert.That(TestCost.Queryable.Count(c => c.PriceCost == price.Costs.Single()), Is.GreaterThan(0), "нет цен");
			}
		}

		[Test]
		public void Enable_price_for_client_with_supplier_client_id()
		{
		}
	}
}