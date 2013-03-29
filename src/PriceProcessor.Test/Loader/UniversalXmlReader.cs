using System.Collections.Generic;
using System.Linq;
using Common.Tools.Test;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Core;
using NUnit.Framework;

namespace PriceProcessor.Test.Loader
{
	[TestFixture]
	public class UniversalXmlReader
	{
		private static List<Customer> settings;
		private static List<FormalizationPosition> positions;

		[SetUp]
		public void Setup()
		{
			settings = null;
			positions = null;
		}

		[Test]
		public void Read_settings()
		{
			var xml = @"<Settings>
<Group>
	<ClientId>122341</ClientId>
	<PayerId>21</PayerId>
	<CostId>0000-Крик_122341</CostId>
	<Markup>0</Markup>
	<Available>1</Available>
	<Address>
		<AddressId>122345</AddressId>
		<ControlMinReq>1</ControlMinReq>
		<MinReq>3000</MinReq>
	</Address>
	<Address>
		<AddressId>274714</AddressId>
		<ControlMinReq>0</ControlMinReq>
	</Address>
</Group>
<Group>
	<ClientId>122221</ClientId>
	<PayerId>21</PayerId>
	<CostId>0000-Крик_122221</CostId>
	<Address>
		<AddressId>122224</AddressId>
		<ControlMinReq>0</ControlMinReq>
		<MinReq>3000</MinReq>
	</Address>
</Group>
</Settings>
";
			Read(xml);

			Assert.That(settings.Count, Is.EqualTo(2));
			var customer = settings[0];
			Assert.That(customer.SupplierClientId, Is.EqualTo("122341"));
			Assert.That(customer.SupplierPaymentId, Is.EqualTo("21"));
			Assert.That(customer.CostId, Is.EqualTo("0000-Крик_122341"));
			Assert.That(customer.Addresses.Count, Is.EqualTo(2));
			var address = customer.Addresses[0];
			Assert.That(address.SupplierAddressId, Is.EqualTo("122345"));
			Assert.That(address.ControlMinReq, Is.True);
			Assert.That(address.MinReq, Is.EqualTo(3000));
		}

		[Test]
		public void Parse_price()
		{
			var xml = @"<Price>
	<Item>
		<Code>1038</Code>
		<Product>Таблетки От Кашля Х10</Product>
		<Producer>Татхимфармпрепараты ОАО</Producer>
		<Cost>
			<Id>P2524</Id>
			<Value>10.50</Value>
		</Cost>
		<Cost>
			<Id>P2526</Id>
			<Value>10</Value>
		</Cost>
	</Item>
	<Item>
		<Code>17390</Code>
		<Product>Иммунал таб. №20</Product>
		<Producer>Lek (аптечка)</Producer>
		<Cost>
			<Id>P2525</Id>
			<Value>50</Value>
		</Cost>
	</Item>
</Price>
";
			Read(xml);

			Assert.That(positions.Count, Is.EqualTo(2));
			var position = positions[0];
			Assert.That(position.PositionName, Is.EqualTo("Таблетки От Кашля Х10"));
			Assert.That(position.FirmCr, Is.EqualTo("Татхимфармпрепараты ОАО"));
			Assert.That(position.Core.Costs.Length, Is.EqualTo(2));
			var cost = position.Core.Costs[0];
			Assert.That(cost.Value, Is.EqualTo(10.50));
			cost = position.Core.Costs[1];
			Assert.That(cost.Value, Is.EqualTo(10));
			Assert.That(positions[1].Core.Costs.Length, Is.EqualTo(1));
		}

		[Test]
		public void Combine_price_and_settings()
		{
			var xml = @"<PriceAndSettings>
	<Settings>
		<Group>
			<ClientId>122221</ClientId>
			<PayerId>21</PayerId>
			<CostId>0000-Крик_122221</CostId>
			<Address>
				<AddressId>122224</AddressId>
				<ControlMinReq>0</ControlMinReq>
				<MinReq>3000</MinReq>
			</Address>
		</Group>
	</Settings>
	<Price>
		<Item>
			<Code>1038</Code>
			<Product>Таблетки От Кашля Х10</Product>
			<Producer>Татхимфармпрепараты ОАО</Producer>
			<Cost>
				<Id>P2524</Id>
				<Value>10.50</Value>
			</Cost>
			<Cost>
				<Id>P2524</Id>
				<Value>10</Value>
			</Cost>
		</Item>
	</Price>
</PriceAndSettings>";

			Read(xml);

			Assert.That(positions.Count, Is.EqualTo(1));
			Assert.That(settings.Count, Is.EqualTo(1));
		}

		[Test]
		public void DublicateCostsTest()
		{
			var xml = @"
<Price>
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

			Read(xml);
			Assert.That(positions[0].Core.Costs.Length, Is.EqualTo(2));
		}

		private static void Read(string xml)
		{
			var reader = new UniversalReader(new StringStream(xml));
			settings = reader.Settings().ToList();
			positions = reader.Read().ToList();
		}
	}
}