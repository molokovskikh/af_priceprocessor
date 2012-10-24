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
using FileHelper = Common.Tools.FileHelper;

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
			using (new SessionScope()) {
				var supplier = TestSupplier.Create();
				var price = supplier.Prices[0];
				var cost = price.Costs[0];
				cost.Name = "116";
				priceItem = cost.PriceItem;
				priceItem.Format.PriceFormat = PriceFormatType.FarmaimpeksXml;
				price.SaveAndFlush();
				Settings.Default.SyncPriceCodes.Add(price.Id.ToString());
				prices.Add(price);

				price.CreateAssortmentBoundSynonyms("Аспирин-С №10 таб.шип.", "Bayer AG, Франция");
				price.CreateAssortmentBoundSynonyms("Абактал 400мг №10 таб.п/о", "Lek, Словения");

				price = new TestPrice(supplier) {
					CostType = CostType.MultiColumn,
					ParentSynonym = price.Id,
				};
				cost = price.Costs[0];
				cost.Name = "2";
				price.SaveAndFlush();
				supplier.Maintain();
				Settings.Default.SyncPriceCodes.Add(price.Id.ToString());
				prices.Add(price);
			}
		}

		[Test]
		public void Load_xml_source()
		{
			Formalize("FarmaimpeksSmallPrice.xml");
			using (new SessionScope())
				foreach (var price in prices) {
					Assert.That(TestCore.Queryable.Count(c => c.Price == price), Is.GreaterThan(0), "нет предложений, прайс {0} {1}", price.PriceName, price.Id);
					Assert.That(TestCost.Queryable.Count(c => c.Id.CostId == price.Costs.Single().Id), Is.GreaterThan(0), "нет цен, прайс {0} {1}", price.PriceName, price.Id);
				}
		}

		[Test]
		public void Enable_price_for_client_with_supplier_client_id()
		{
			var trustedClient = TestClient.Create();
			var normalClient = TestClient.Create();
			var price = prices[1];
			using (new SessionScope()) {
				var trustedIntersection = TestIntersection.Queryable.Single(i => i.Price == price && i.Client == trustedClient);
				trustedIntersection.SupplierClientId = "3273";
				trustedIntersection.PriceMarkup = -1;
				trustedIntersection.Save();
			}

			Formalize("FarmaimpeksSmallPrice.xml");
			using (new SessionScope()) {
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
			var price = TestPrice.Find(prices[1].Id);
			Assert.That(price.PriceName, Is.EqualTo("Прайс Опт ДП"));
		}

		[Test]
		public void Get_all_names()
		{
			var basepath = Settings.Default.BasePath;
			if (!Directory.Exists(basepath))
				Directory.CreateDirectory(basepath);

			var source = Path.GetFullPath(@"..\..\Data\FarmaimpeksPrice.xml");
			var destination = Path.GetFullPath(Path.Combine(basepath, priceItem.Id.ToString() + ".xml"));
			File.Copy(source, destination);

			var item = PriceProcessItem.GetProcessItem(priceItem.Id);
			var names = item.GetAllNames();
			Assert.That(names.Count(), Is.EqualTo(9818));
		}

		private void Formalize(string file)
		{
			var formalizer = PricesValidator.Validate(Path.Combine(@"..\..\Data\", file), Path.GetTempFileName(), priceItem.Id);
			formalizer.Formalize();
		}
	}
}