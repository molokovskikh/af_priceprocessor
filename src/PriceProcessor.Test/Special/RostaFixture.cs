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
		public void DownloadPrice(string key, string price, string producers)
		{
			File.Copy(@"..\..\Data\Rosta\price", price);
			File.Copy(@"..\..\Data\Rosta\producers", producers);
		}
	}

	[TestFixture]
	public class RostaFixture
	{
		[Test, Ignore("Для тестирования руками, часто не запускать что бы не спалиться")]
		public void Download_price()
		{
			var downloader = new RostaDownloader();
			downloader.DownloadPrice("20100111151207-390-12", "price", "producers");
		}

		[Test]
		public void Decode()
		{
			Assert.That(RostaDecoder.GetKey("6B020101000100000004040302071A1E0A091D1C03"), Is.EqualTo("20100111151207-390-12"));
		}

		[Test]
		public void Try_to_formalize_price_for_new_client()
		{
			Setup.Initialize();
			var price = TestPrice.CreateTestPrice(CostType.MultiFile);
			var cost = price.Costs.First();
			var format = cost.PriceItem.Format;
			format.FCode = "F1";
			format.FName1 = "F2";
			format.FFirmCr = "F5";
			format.FBaseCost = "F3";
			format.FQuantity = "F4";
			format.Update();
			price.ParentSynonym = 216;
			price.Update();
			var rule = price.Costs.First().FormRule;
			rule.FieldName = "F3";
			rule.Update();

			var client = TestOldClient.CreateTestClient(524288UL);

			BasicConfigurator.Configure();
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
			Thread.Sleep(10.Second());
			var costs = TestHelper.Fill(String.Format("select * from Farm.CoreCosts where PC_CostCode = {0}", price.Costs.Last().Id));
			Assert.That(costs.Tables[0].Rows.Count, Is.GreaterThan(0));
		}
	}
}
