﻿using System;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools.Test;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Formalizer;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;
using Test.Support.log4net;

namespace PriceProcessor.Test.Loader
{
	[TestFixture]
	public class UniversalFormalizerFixture
	{
		private string xml;
		private string file;
		private TestPriceItem priceItem;
		private TestPrice price;

		[SetUp]
		public void Setup()
		{
			xml = @"<Price>
	<Item>
		<Code>109054</Code>
		<Product>Маска трехслойная на резинках медицинская Х3 Инд. уп. И/м</Product>
		<Producer>Вухан Лифарма Кемикалз Ко</Producer>
		<Volume>400</Volume>
		<Quantity>296</Quantity>
		<Period>01.01.2013</Period>
		<VitallyImportant>0</VitallyImportant>
		<NDS>10</NDS>
		<RequestRatio>20</RequestRatio>
		<Cost>
			<Id>PRICE6</Id>
			<Value>10.0</Value>
			<MinOrderCount>20</MinOrderCount>
		</Cost>
		<Cost>
			<Id>PRICE1</Id>
			<Value>10.0</Value>
			<MinOrderCount>20</MinOrderCount>
		</Cost>
	</Item>
</Price>
";

			var supplier = TestSupplier.Create();
			using (new SessionScope()) {
				price = supplier.Prices[0];
				priceItem = price.Costs[0].PriceItem;
				var format = priceItem.Format;
				format.PriceFormat = PriceFormatType.UniversalXml;
				format.Save();

				price.CreateAssortmentBoundSynonyms("Маска трехслойная на резинках медицинская Х3 Инд. уп. И/м", "Вухан Лифарма Кемикалз Ко");
				price.Save();
			}
		}

		[TearDown]
		public void TearDown()
		{
			if (File.Exists(file))
				File.Delete(file);
		}

		[Test]
		public void Create_new_costs()
		{
			Formalize();

			using (new SessionScope()) {
				price = TestPrice.Find(price.Id);
				var cores = price.Core;
				Assert.That(cores.Count, Is.EqualTo(1));
			}
		}

		[Test]
		public void Apply_settings()
		{
			xml = @"<PriceAndSettings>
	<Settings>
		<Group>
			<ClientId>122221</ClientId>
			<PayerId>21</PayerId>
			<CostId>PRICE6</CostId>
			<Markup>10</Markup>
			<Available>1</Available>
			<Address>
				<AddressId>122224</AddressId>
				<ControlMinReq>0</ControlMinReq>
				<MinReq>3000</MinReq>
			</Address>
		</Group>
	</Settings>
	<Price>
		<Item>
			<Code>109054</Code>
			<Product>Маска трехслойная на резинках медицинская Х3 Инд. уп. И/м</Product>
			<Producer>Вухан Лифарма Кемикалз Ко</Producer>
			<Volume>400</Volume>
			<Quantity>296</Quantity>
			<Period>01.01.2013</Period>
			<VitallyImportant>0</VitallyImportant>
			<NDS>10</NDS>
			<RequestRatio>20</RequestRatio>
			<Cost>
				<Id>PRICE6</Id>
				<Value>10.10</Value>
				<MinOrderCount>20</MinOrderCount>
			</Cost>
			<Cost>
				<Id>PRICE1</Id>
				<Value>10.0</Value>
				<MinOrderCount>20</MinOrderCount>
			</Cost>
		</Item>
	</Price>
</PriceAndSettings>";
			var client = TestClient.Create();

			TestIntersection intersection;
			using(new SessionScope()) {
				var intersections = TestIntersection.Queryable.Where(i => i.Client == client && i.Price == price).ToList();
				Assert.That(intersections.Count, Is.EqualTo(1));
				intersection = intersections[0];
				intersection.SupplierClientId = "122221";
				intersection.SupplierPaymentId = "21";
				intersection.AddressIntersections[0].SupplierDeliveryId = "122224";
			}

			Formalize();

			using(new SessionScope()) {
				intersection = TestIntersection.Find(intersection.Id);
				Assert.That(intersection.Cost.Name, Is.EqualTo("PRICE6"));
				Assert.That(intersection.AvailableForClient, Is.True);
				Assert.That(intersection.PriceMarkup, Is.EqualTo(10));
				var addressintersection = intersection.AddressIntersections[0];
				Assert.That(addressintersection.MinReq, Is.EqualTo(3000));
			}
		}

		[Test]
		public void Create_costs()
		{
			Formalize();

			using (new SessionScope()) {
				price = TestPrice.Find(price.Id);

				Assert.That(price.Costs.Count, Is.EqualTo(3));
				Assert.That(price.Costs[1].Name, Is.EqualTo("PRICE6"));
				Assert.That(price.Costs[2].Name, Is.EqualTo("PRICE1"));
			}
		}

		[Test]
		public void Check_core_loading()
		{
			Settings.Default.SyncPriceCodes.Add(price.Id.ToString());

			Formalize();

			int firstCoreCount;
			using (new SessionScope()) {
				price = TestPrice.Find(price.Id);

				Assert.That(price.Core.Count, Is.GreaterThan(0));
				firstCoreCount = price.Core.Count;
				var core = price.Core[0];
				core.CodeOKP = null;
				core.Save();
			}

			Formalize();

			using (new SessionScope()) {
				price = TestPrice.Find(price.Id);

				Assert.That(price.Core.Count, Is.EqualTo(firstCoreCount), "Количество позиций после повторной формализации должно совпадать с первоначальной формализацией");
			}
		}

		[Test(Description = "UniversalFormalizer всегда обновляет позиции в Core")]
		public void DoubleFormalize()
		{
			//если строка "_priceInfo.IsUpdating = true;" в UniversalFormalizer будет удалена, 
			//а BasePriceParser2 не будет починен, то этот тест должен поломаться.
			Formalize();

			int firstCoreCount;
			using (new SessionScope()) {
				price = TestPrice.Find(price.Id);

				Assert.That(price.Core.Count, Is.GreaterThan(0));
				firstCoreCount = price.Core.Count;
			}

			Formalize();

			using (new SessionScope()) {
				price = TestPrice.Find(price.Id);

				Assert.That(price.Core.Count, Is.EqualTo(firstCoreCount), "Количество позиций после повторной формализации должно совпадать с первоначальной формализацией");
			}
		}

		[Test]
		public void Save_additional_cost_columns()
		{
			Formalize();

			using (new SessionScope()) {
				price = TestPrice.Find(price.Id);
				var cost = price.Core[0].Costs[0];
				Assert.That(cost.MinOrderCount, Is.EqualTo(20));
			}
		}

		[Test]
		public void Formalize_position_without_costs()
		{
			xml = @"<Price>
	<Item>
		<Code>109054</Code>
		<Product>Маска трехслойная на резинках медицинская Х3 Инд. уп. И/м</Product>
		<Producer>Вухан Лифарма Кемикалз Ко</Producer>
		<Volume>400</Volume>
		<Quantity>296</Quantity>
		<Period>01.01.2013</Period>
		<VitallyImportant>0</VitallyImportant>
		<NDS>10</NDS>
		<RequestRatio>20</RequestRatio>
		<Cost>
			<Id>PRICE1</Id>
			<Value>0</Value>
		</Cost>
	</Item>
</Price>";
			Formalize();

			using (new SessionScope()) {
				price = TestPrice.Find(price.Id);
				Assert.That(price.Core.Count, Is.EqualTo(0));
			}
		}

		[Test]
		public void Formalize_duplicate_postions()
		{
			xml = @"<Price>
	<Item>
		<Code>109054</Code>
		<Product>Маска трехслойная на резинках медицинская Х3 Инд. уп. И/м</Product>
		<Producer>Вухан Лифарма Кемикалз Ко</Producer>
		<Volume>400</Volume>
		<Quantity>296</Quantity>
		<Period>01.01.2013</Period>
		<VitallyImportant>0</VitallyImportant>
		<NDS>10</NDS>
		<RequestRatio>20</RequestRatio>
		<Cost>
			<Id>PRICE1</Id>
			<Value>100</Value>
		</Cost>
	</Item>
</Price>";
			Formalize();

			xml = @"<Price>
	<Item>
		<Code>109054</Code>
		<Product>Маска трехслойная на резинках медицинская Х3 Инд. уп. И/м</Product>
		<Producer>Вухан Лифарма Кемикалз Ко</Producer>
		<Volume>400</Volume>
		<Quantity>296</Quantity>
		<Period>01.01.2013</Period>
		<VitallyImportant>0</VitallyImportant>
		<NDS>10</NDS>
		<RequestRatio>20</RequestRatio>
		<Cost>
			<Id>PRICE2</Id>
			<Value>100.50</Value>
		</Cost>
	</Item>
	<Item>
		<Code>109054</Code>
		<Product>Маска трехслойная на резинках медицинская Х3 Инд. уп. И/м</Product>
		<Producer>Вухан Лифарма Кемикалз Ко</Producer>
		<Volume>400</Volume>
		<Quantity>5</Quantity>
		<Period>01.01.2013</Period>
		<VitallyImportant>0</VitallyImportant>
		<NDS>10</NDS>
		<RequestRatio>20</RequestRatio>
		<Cost>
			<Id>PRICE2</Id>
			<Value>100.30</Value>
		</Cost>
	</Item>
</Price>";
			Formalize();

			using (new SessionScope()) {
				price = TestPrice.Find(price.Id);
				Assert.That(price.Core.Count, Is.EqualTo(2));
			}
		}

		[Test]
		public void Alert_test()
		{
			var logFile = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.log").FirstOrDefault(f => f.Contains("PriceProcessor_"));
			if (!string.IsNullOrEmpty(logFile) && File.Exists(logFile))
				File.Delete(logFile);

			xml = @"<Price>
	<Item>
		<Code>109054</Code>
		<Product>Маска трехслойная на резинках медицинская Х3 Инд. уп. И/м</Product>
		<Producer>Вухан Лифарма Кемикалз Ко</Producer>
		<Volume>400</Volume>
		<Quantity>296</Quantity>
		<Period>01.01.2013</Period>
		<VitallyImportant>0</VitallyImportant>
		<NDS>10</NDS>
		<RequestRatio>20</RequestRatio>
		<Cost>
			<Id>PRICE2</Id>
			<Value>0</Value>
		</Cost>
	</Item>
	<Item>
		<Code>109055</Code>
		<Product>Маска трехслойная на резинках медицинская Х3 Инд. уп. И/м</Product>
		<Producer>Вухан Лифарма Кемикалз Ко</Producer>
		<Volume>400</Volume>
		<Quantity>5</Quantity>
		<Period>01.01.2013</Period>
		<VitallyImportant>0</VitallyImportant>
		<NDS>10</NDS>
		<RequestRatio>20</RequestRatio>
		<Cost>
			<Id>Тестовая</Id>
			<Value>-1</Value>
		</Cost>
	</Item>
</Price>";
			Formalize();

			Assert.IsNotNullOrEmpty(logFile);
			var text = File.ReadAllText(logFile);
			Assert.That(text, Is.StringContaining("имеются ценовые колонки, полностью заполненные ценой '0'."));
			Assert.That(text, Is.StringContaining("имеются позиции с незаполненными ценами."));
		}

		private void Formalize()
		{
			file = Path.GetTempFileName();
			File.WriteAllText(file, xml);
			var formalizer = PricesValidator.Validate(file, Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()),
				priceItem.Id);
			formalizer.Downloaded = true;
			formalizer.Formalize();
		}

		[Test]
		public void DublicateCostsTest()
		{
			xml =
				@"<Price>
<Item>
<Code>185</Code>
<Product>Маска трехслойная на резинках медицинская Х3 Инд. уп. И/м</Product>
<Producer>>Вухан Лифарма Кемикалз Ко</Producer>
<Unit>шт.</Unit>
<Volume>42,112</Volume>
<Quantity>8</Quantity>
<Period>15.05.2012</Period>
<Junk>0</Junk>
<VitallyImportant>0</VitallyImportant><NDS>10</NDS><RegistryCost>0</RegistryCost><ProducerCost>0</ProducerCost><RequestRatio>0</RequestRatio>
<Cost><Id>P10094549</Id><Value>34.55</Value><MinOrderCount>3</MinOrderCount></Cost>
<Cost><Id>P10094549</Id><Value>32.59</Value><MinOrderCount>6</MinOrderCount></Cost>
<Cost><Id>P10094674</Id><Value>35.08</Value><MinOrderCount>3</MinOrderCount></Cost>
</Item>
</Price>";

			var reader = new UniversalReader(new StringStream(xml));
			var positions = reader.Read().ToList();
			Assert.That(positions[0].Core.Costs.Length, Is.EqualTo(2));
		}
	}
}