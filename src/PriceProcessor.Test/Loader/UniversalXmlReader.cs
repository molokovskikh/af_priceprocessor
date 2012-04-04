using System.Linq;
using Common.Tools.Test;
using Inforoom.PriceProcessor.Formalizer;
using NUnit.Framework;

namespace PriceProcessor.Test.Loader
{
	[TestFixture]
	public class UniversalXmlReader
	{
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
			var reader = Read(xml);

			var customers = reader.Settings().ToList();
			Assert.That(customers.Count, Is.EqualTo(2));
			var customer = customers[0];
			Assert.That(customer.SupplierClientId, Is.EqualTo("122341"));
			Assert.That(customer.SupplierPayerId, Is.EqualTo("21"));
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
			<Id>P2524</Id>
			<Value>10</Value>
		</Cost>
	</Item>
	<Item>
		<Code>17390</Code>
		<Product>Иммунал таб. №20</Product>
		<Producer>Lek (аптечка)</Producer>
		<Cost>
			<Id>P2524</Id>
			<Value>50</Value>
		</Cost>
	</Item>
</Price>
";
			var reader = Read(xml);

			var positions = reader.Read().ToList();
			Assert.That(positions.Count, Is.EqualTo(2));
			var position = positions[0];
			Assert.That(position.PositionName, Is.EqualTo("Таблетки От Кашля Х10"));
			Assert.That(position.FirmCr, Is.EqualTo("Татхимфармпрепараты ОАО"));
			Assert.That(position.Core.Costs.Length, Is.EqualTo(2));
			var cost = position.Core.Costs[0];
			Assert.That(cost.Value, Is.EqualTo(10.50));
			cost = position.Core.Costs[1];
			Assert.That(cost.Value, Is.EqualTo(10));
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

			var reader = Read(xml);

			var settings = reader.Settings().ToList();
			var positions = reader.Read().ToList();
			Assert.That(positions.Count, Is.EqualTo(1));
			Assert.That(settings.Count, Is.EqualTo(1));
		}

		private static UniversalReader Read(string xml)
		{
			return new UniversalReader(new StringStream(xml));
		}
	}
}