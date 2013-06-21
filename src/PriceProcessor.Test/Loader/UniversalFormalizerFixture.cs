﻿using System;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Tools.Test;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Core;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;
using Test.Support.log4net;
using log4net.Core;

namespace PriceProcessor.Test.Loader
{
	[TestFixture]
	public class UniversalFormalizerFixture : IntegrationFixture
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
			price = supplier.Prices[0];
			priceItem = price.Costs[0].PriceItem;
			var format = priceItem.Format;
			format.PriceFormat = PriceFormatType.UniversalXml;
			format.Save();

			price.CreateAssortmentBoundSynonyms("Маска трехслойная на резинках медицинская Х3 Инд. уп. И/м", "Вухан Лифарма Кемикалз Ко");
			price.Save();
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

			session.Refresh(price);
			var cores = price.Core;
			Assert.That(cores.Count, Is.EqualTo(1));
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

			var intersections = TestIntersection.Queryable.Where(i => i.Client == client && i.Price == price).ToList();
			Assert.That(intersections.Count, Is.EqualTo(1));
			var intersection = intersections[0];
			intersection.SupplierClientId = "122221";
			intersection.SupplierPaymentId = "21";
			intersection.AddressIntersections[0].SupplierDeliveryId = "122224";

			Formalize();

			session.Refresh(intersection);
			Assert.That(intersection.Cost.Name, Is.EqualTo("PRICE6"));
			Assert.That(intersection.AvailableForClient, Is.True);
			Assert.That(intersection.PriceMarkup, Is.EqualTo(10));
			var addressintersection = intersection.AddressIntersections[0];
			session.Refresh(addressintersection);
			Assert.That(addressintersection.MinReq, Is.EqualTo(3000));
		}

		[Test]
		public void Create_costs()
		{
			Formalize();

			session.Refresh(price);
			Assert.That(price.Costs.Count, Is.EqualTo(3));
			Assert.That(price.Costs[1].Name, Is.EqualTo("PRICE6"));
			Assert.That(price.Costs[2].Name, Is.EqualTo("PRICE1"));
		}

		[Test]
		public void Check_core_loading()
		{
			Settings.Default.SyncPriceCodes.Add(price.Id.ToString());

			Formalize();

			Assert.That(price.Core.Count, Is.GreaterThan(0));
			var firstCoreCount = price.Core.Count;
			var core = price.Core[0];
			core.CodeOKP = 0;
			core.Save();

			Formalize();

			session.Refresh(price);
			Assert.That(price.Core.Count, Is.EqualTo(firstCoreCount), "Количество позиций после повторной формализации должно совпадать с первоначальной формализацией");
		}

		[Test(Description = "UniversalFormalizer всегда обновляет позиции в Core")]
		public void DoubleFormalize()
		{
			//если строка "_priceInfo.IsUpdating = true;" в UniversalFormalizer будет удалена, 
			//а BasePriceParser2 не будет починен, то этот тест должен поломаться.
			Formalize();

			session.Refresh(price);

			Assert.That(price.Core.Count, Is.GreaterThan(0));
			var firstCoreCount = price.Core.Count;

			Formalize();

			session.Refresh(price);

			Assert.That(price.Core.Count, Is.EqualTo(firstCoreCount), "Количество позиций после повторной формализации должно совпадать с первоначальной формализацией");
		}

		[Test]
		public void Save_additional_cost_columns()
		{
			Formalize();

			session.Refresh(price);
			var cost = price.Core[0].Costs[0];
			Assert.That(cost.MinOrderCount, Is.EqualTo(20));
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

			session.Refresh(price);
			Assert.That(price.Core.Count, Is.EqualTo(0));
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

			session.Refresh(price);
			Assert.That(price.Core.Count, Is.EqualTo(2));
		}

		[Test]
		public void Alert_test()
		{
			var filter = new EventFilter<Alerts>(Level.Debug);

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

			Assert.That(filter.Events.Count, Is.EqualTo(2));
		}

		[Test]
		public void Sync_cost_settings_for_client()
		{
			xml = @"<PriceAndSettings>
	<Settings>
		<Group>
			<ClientId>122221</ClientId>
			<PayerId>21</PayerId>
			<CostId>PRICE6</CostId>
			<Markup>10</Markup>
			<Available>1</Available>
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
		</Item>
	</Price>
</PriceAndSettings>";

			var client = TestClient.CreateNaked();
			client.CreateLegalEntity();
			client.MaintainIntersection();

			var intersections = TestIntersection.Queryable.Where(i => i.Client == client && i.Price == price).ToList();
			Assert.That(intersections.Count, Is.EqualTo(2));
			var intersection = intersections[0];
			intersection.SupplierClientId = "122221";
			intersection.SupplierPaymentId = "21";

			Formalize();

			session.Refresh(intersection);
			Assert.That(intersection.Cost.Name, Is.EqualTo("PRICE6"), "{0} - {1}", client.Id, price.Id);
			intersection = intersections[1];
			session.Refresh(intersection);
			Assert.That(intersection.Cost.Name, Is.EqualTo("PRICE6"), "{0} - {1}", client.Id, price.Id);
		}

		[Test]
		public void Assortment_price()
		{
			price.PriceType = PriceType.Assortment;
			price.Save();

			xml = @"<Price>
	<Item>
		<Code>109054</Code>
		<Product>Маска трехслойная на резинках медицинская Х3 Инд. уп. И/м</Product>
		<Producer>Вухан Лифарма Кемикалз Ко</Producer>
	</Item>
	<Item>
		<Code>109055</Code>
		<Product>Маска трехслойная на резинках медицинская Х3 Инд. уп. И/м</Product>
		<Producer>Вухан Лифарма Кемикалз Ко</Producer>
	</Item>
</Price>";

			Formalize();
			xml = @"<Price>
	<Item>
		<Code>109054</Code>
		<Product>Маска трехслойная на резинках медицинская Х3 Инд. уп. И/м</Product>
		<Producer>Вухан Лифарма Кемикалз Ко</Producer>
		<Quantity>10</Quantity>
	</Item>
	<Item>
		<Code>109055</Code>
		<Product>Маска трехслойная на резинках медицинская Х3 Инд. уп. И/м</Product>
		<Producer>Вухан Лифарма Кемикалз Ко</Producer>
	</Item>
</Price>";
			Formalize();

			session.Refresh(price);
			Assert.That(price.Core.Count, Is.EqualTo(2));
		}

		private void Formalize()
		{
			session.Flush();
			if (session.Transaction.IsActive)
				session.Transaction.Commit();

			file = Path.GetTempFileName();
			File.WriteAllText(file, xml);
			var formalizer = PricesValidator.Validate(file, Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()),
				priceItem.Id);
			formalizer.Downloaded = true;
			formalizer.Formalize();
		}
	}
}