using System;
using System.IO;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Tools.Calendar;
using Inforoom.PriceProcessor.Rosta;
using log4net.Config;
using NUnit.Framework;
using Test.Support;

namespace PriceProcessor.Test.Special
{
	public class FakeDownloader : IDownloader
	{
		public string Hwinfo;

		public string Host { get; set; }

		public IAdditionReader AdditionReader { get; set; }

		public void DownloadPrice(string key, string hwinfo, string price, string producers, string ex)
		{
			Hwinfo = hwinfo;
			File.Copy(@"..\..\Data\Rosta\price", price);
			File.Copy(@"..\..\Data\Rosta\producers", producers);
			File.Copy(@"..\..\Data\Rosta\ex", ex);
		}
	}

	[TestFixture]
	public class RostaFixture
	{
		private TestClient client;
		private TestPrice price;
		private RostaHandler handler;
		private FakeDownloader downloader;

		[SetUp]
		public void SetUp()
		{
			Setup.Initialize();

			SystemTime.Now = () => DateTime.Now;
			using (var scope = new TransactionScope(OnDispose.Rollback))
			{
				price = CreatePriceForRosta();
				scope.VoteCommit();
			}

			client = TestClient.Create(TestRegion.Inforoom);
		}

		public static TestPrice CreatePriceForRosta()
		{
			var price = TestPrice.CreateTestPrice(CostType.MultiFile, "F3");
			var cost = price.Costs.First();
			var format = cost.PriceItem.Format;
			format.FCode = "F1";
			format.FName1 = "F2";
			format.FFirmCr = "F5";
			format.FBaseCost = "F3";
			format.FQuantity = "F4";
			format.FPeriod = "Period";
			format.Update();
			price.ParentSynonym = 216;
			price.Update();
			return price;
		}

		[Test, Ignore("Сломан тк больше не хранится нужная информация для росты пусть належится пару меяцев а затем удалить")]
		public void Try_to_formalize_price_for_new_client()
		{
			TestHelper.Execute(@"update Usersettings.Intersection set InvisibleOnClient = 1 where pricecode = {0}", price.Id);
			TestHelper.Execute(@"update Usersettings.Intersection set InvisibleOnClient = 0 where pricecode = {0} and clientcode = {1}", price.Id, client.Id);
			TestHelper.Execute(@"insert into logs.SpyInfo(UserId, RostaUin) values ({0}, '6B020101000100000004040302071A1E0A091D1C03')", client.Users.First().Id);
			var handler = new RostaHandler(price.Id, new FakeDownloader());
			handler.SleepTime = 1;

			handler.StartWork();
			Thread.Sleep(3.Second());
			using (new SessionScope())
			{
				price = TestPrice.Find(price.Id);
				Assert.That(price.Costs.Count, Is.EqualTo(2));
				Assert.That(price.Costs[1].BaseCost, Is.False);
				Assert.That(price.Costs[1].Name, Is.EqualTo("20100111151207-390-12"));
			}

			SystemTime.Now = () => DateTime.Now + 1.Hour();
			Thread.Sleep(20.Second());
			var costs = TestHelper.Fill(String.Format("select * from Farm.CoreCosts where PC_CostCode = {0}", price.Costs.Last().Id));
			Assert.That(costs.Tables[0].Rows.Count, Is.GreaterThan(0));
			handler.StopWork();
		}

        [Test, Ignore("Починить")]
		public void Parser_should_read_period()
		{
			TestHelper.Execute(@"
update Future.Intersection set AvailableForClient = 0 where PriceId = {0};
update Future.Intersection i
join Future.AddressIntersection ai on ai.IntersectionId = i.Id
set i.AvailableForClient = 1, SupplierClientId = '20100111151207-390-12', ai.SupplierDeliveryId = '00000F65-00020800-0000E49D-BFEBFBFF-605B5101-007D7040-GenuineIntel', i.SupplierPaymentId = '02/05/2007-I945-6A79TG0AC-00'
where i.PriceId = {0} and i.ClientId = {1}", price.Id, client.Id);

			Process();
			AssertThatFormalized();
			var costs = TestHelper.Fill(String.Format("select * from Farm.Core0 where PriceCode = {0}", price.Id));
			Assert.That(costs.Tables[0].Rows[0]["Period"], Is.Not.EqualTo(""));
		}

        [Test, Ignore("Починить")]
		public void Create_client_with_only_cpuid()
		{
			TestHelper.Execute(@"
update Future.Intersection set AvailableForClient = 0 where PriceId = {0};
update Future.Intersection i
join Future.AddressIntersection ai on ai.IntersectionId = i.Id
set i.AvailableForClient = 1, SupplierClientId = '20100111151207-390-12', ai.SupplierDeliveryId = '00000F65-00020800-0000E49D-BFEBFBFF-605B5101-007D7040-GenuineIntel', i.SupplierPaymentId = ''
where i.PriceId = {0} and i.ClientId = {1}", price.Id, client.Id);

			Process();
			AssertThatFormalized();
			Assert.That(downloader.Hwinfo, Is.EqualTo("00000F65-00020800-0000E49D-BFEBFBFF-605B5101-007D7040-GenuineIntel\r\n"));
		}

        [Test, Ignore("Починить")]
		public void Create_new_cost_column_if_rosta_uin_configured_but_base_cost_not_set()
		{
			TestHelper.Execute(@"
update Future.Intersection set AvailableForClient = 0 where PriceId = {0};
update Future.Intersection i
join Future.AddressIntersection ai on ai.IntersectionId = i.Id
set i.AvailableForClient = 1, SupplierClientId = '20100111151207-390-12', ai.SupplierDeliveryId = '00000F65-00020800-0000E49D-BFEBFBFF-605B5101-007D7040-GenuineIntel', i.SupplierPaymentId = '02/05/2007-I945-6A79TG0AC-00'
where i.PriceId = {0} and i.ClientId = {1}", price.Id, client.Id);

			ProcessOnce();

			using (new SessionScope())
			{
				price = TestPrice.Find(price.Id);
				Assert.That(price.Costs.Count, Is.EqualTo(2));
				Assert.That(price.Costs[1].BaseCost, Is.False);
				Assert.That(price.Costs[1].Name, Is.EqualTo("20100111151207-390-12"));
			}

		}

		[Test, Ignore("Починить")]
		public void Configure_client_from_future()
		{
			TestHelper.Execute(@"
update Future.Intersection set AvailableForClient = 0 where PriceId = {0};
update Future.Intersection i
join Future.AddressIntersection ai on ai.IntersectionId = i.Id
set i.AvailableForClient = 1, SupplierClientId = '20100111151207-390-12', ai.SupplierDeliveryId = '00000F65-00020800-0000E49D-BFEBFBFF-605B5101-007D7040-GenuineIntel', i.SupplierPaymentId = '02/05/2007-I945-6A79TG0AC-00'
where i.PriceId = {0} and i.ClientId = {1}", price.Id, client.Id);

			ProcessOnce();

			using (new SessionScope())
			{
				price = TestPrice.Find(price.Id);
				Assert.That(price.Costs.Count, Is.EqualTo(2), "не создали ценовую колонку");
				Assert.That(price.Costs[1].BaseCost, Is.False);
				Assert.That(price.Costs[1].Name, Is.EqualTo("20100111151207-390-12"));

				var intersection = TestIntersection.Queryable.First(i => i.Client == client && i.Price == price);
				Assert.That(intersection.AvailableForClient, Is.True);
				Assert.That(intersection.Cost, Is.EqualTo(price.Costs[1]));
			}
		}

		private void ProcessOnce()
		{
			var downloader = new FakeDownloader();
			var handler = new RostaHandler(price.Id, downloader);

			handler.Process();
		}

		private void AssertThatFormalized()
		{
			var costs = TestHelper.Fill(String.Format("select * from Farm.Core0 where PriceCode = {0}", price.Id));
			Assert.That(costs.Tables[0].Rows.Count, Is.GreaterThan(0));
			Assert.That(costs.Tables[0].Rows[0]["Period"], Is.Not.EqualTo(""));
			handler.StopWork();
		}

		private void Process()
		{
			downloader = new FakeDownloader();
			handler = new RostaHandler(price.Id, downloader);
			handler.SleepTime = 1;
			handler.StartWork();

			Thread.Sleep(3.Second());
			SystemTime.Now = () => DateTime.Now + 1.Hour();
			Thread.Sleep(20.Second());
		}
	}
}
