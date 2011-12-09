using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;
using Inforoom.Common;
using FileHelper = Inforoom.Common.FileHelper;

namespace PriceProcessor.Test.Loader
{
	[TestFixture]
	public class FarmaimpeksFormalizerFixture
	{
		private List<TestPrice> prices;
		private TestPriceItem priceItem;

		[SetUp]
		public void Setup()
		{
			prices = new List<TestPrice>();
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

				var supplier = TestSupplier.Create();
				var price = new TestPrice(supplier) {
					CostType = CostType.MultiColumn,
					ParentSynonym = 4745,
					PriceName = "2"
				};
				var cost = price.NewPriceCost(priceItem, "123");
				cost.Name = "2";
				price.SaveAndFlush();
				Settings.Default.SyncPriceCodes.Add(price.Id.ToString());
				prices.Add(price);

				price = new TestPrice(supplier) {
					CostType = CostType.MultiColumn,
					ParentSynonym = 4745,
					PriceName = "11"
				};
				cost = price.NewPriceCost(priceItem, "123");
				cost.Name = "11";
				price.SaveAndFlush();
				supplier.Maintain();
				Settings.Default.SyncPriceCodes.Add(price.Id.ToString());
				prices.Add(price);
			}
		}

		[Test]
		public void Load_xml_source()
		{
			Formalize("FarmaimpeksPrice.xml");
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
			var trustedClient = TestClient.Create();
			var normalClient = TestClient.Create();
			var price = prices[1];
			using(new SessionScope())
			{
				var trustedIntersection = TestIntersection.Queryable.Single(i => i.Price == price && i.Client == trustedClient);
				trustedIntersection.SupplierClientId = "4873";
				trustedIntersection.PriceMarkup = -1;
				trustedIntersection.Save();
			}

			Formalize("FarmaimpeksPrice.xml");
			using(new SessionScope())
			{
				var regularIntersection = TestIntersection.Queryable.Single(i => i.Price == price && i.Client == normalClient);
				Assert.That(regularIntersection.AvailableForClient, Is.False);

				var trustedIntersection = TestIntersection.Queryable.Single(i => i.Price == price && i.Client == trustedClient);
				Assert.That(trustedIntersection.AvailableForClient, Is.True);
			}
		}

		[Test]
		public void Update_price_name_from_source_file()
		{
			Formalize("FarmaimpeksSmallPrice.xml");
			var price = TestPrice.Find(prices[0].Id);
			Assert.That(price.PriceName, Is.EqualTo("Прайс Опт ДП"));
		}

		private void Formalize(string file)
		{
			var formalizer = PricesValidator.Validate(Path.Combine(@"..\..\Data\", file), Path.GetTempFileName(), priceItem.Id);
			formalizer.Formalize();
		}

		[Test]
		public void GetAllNamesTest()
		{
			var basepath = FileHelper.NormalizeDir(Settings.Default.BasePath);
			if (!Directory.Exists(basepath)) Directory.CreateDirectory(basepath);
			File.Copy(Path.GetFullPath(@"..\..\Data\FarmaimpeksPrice.xml"), Path.GetFullPath(@"..\..\Data\FarmaimpeksPrice_tmp.xml"));
			File.Move(Path.GetFullPath(@"..\..\Data\FarmaimpeksPrice_tmp.xml"),
					  Path.GetFullPath(String.Format(@"{0}{1}.xml", basepath, priceItem.Id)));
			var item = PriceProcessItem.GetProcessItem(priceItem.Id);
			var names = item.GetAllNames();
			File.Delete(Path.GetFullPath(String.Format(@"{0}{1}.xml", basepath, priceItem.Id)));
			Assert.That(names.Count(), Is.EqualTo(9286));
		}
	}
}