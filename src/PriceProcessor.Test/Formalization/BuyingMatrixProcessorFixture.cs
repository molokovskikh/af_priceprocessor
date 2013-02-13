using System;
using Common.MySql;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Models;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;

namespace PriceProcessor.Test.Formalization
{
	[TestFixture]
	public class BuyingMatrixProcessorFixture : BaseFormalizationFixture
	{
		private Price localPrice;

		[SetUp]
		public void Setup()
		{
			CreatePrice();

			localPrice = session.Load<Price>(price.Id);
			localPrice.Matrix = new Matrix();
			session.Save(localPrice.Matrix);
			session.Save(localPrice);
		}

		[Test]
		public void Buying_matrix_should_update()
		{
			FormalizeDefaultData();

			With.Connection(c => {
				var command = new MySqlCommand("select count(*) from farm.BuyingMatrix where PriceId = ?Priceid", c);
				command.Parameters.AddWithValue("?PriceId", price.Id);
				var count = Convert.ToUInt32(command.ExecuteScalar());
				Assert.That(count, Is.GreaterThan(0));
			});
		}

		[Test]
		public void Intersect_with_oktp_catalog()
		{
			var okpPrice = new TestPrice(price.Supplier);
			okpPrice.CreateAssortmentBoundSynonyms(
				"9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ",
				"Валента Фармацевтика/Королев Ф");
			var core = new TestCore(okpPrice.ProductSynonyms[0], okpPrice.ProducerSynonyms[0]) {
				CodeOKP = 1
			};
			session.Save(okpPrice);
			session.Save(core);
			session.Flush();
			var localOkpPrice = session.Load<Price>(okpPrice.Id);
			localPrice.CodeOkpFilterPrice  = localOkpPrice;

			priceItem.Format.FCodeOkp = "F5";
			session.Save(localPrice);

			CreateDefaultSynonym();
			Formalize(@"9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ;Валента Фармацевтика/Королев Ф;2864;220.92;1;
5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г;Санкт-Петербургская ф.ф.;24;73.88;;
911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ;Твинс Тэк;40;44.71;;");

			With.Connection(c => {
				var command = new MySqlCommand("select count(*) from farm.BuyingMatrix where PriceId = ?Priceid", c);
				command.Parameters.AddWithValue("?PriceId", price.Id);
				var count = Convert.ToUInt32(command.ExecuteScalar());
				Assert.That(count, Is.EqualTo(1), "код прайс листа {0}", price.Id);
			});
		}

		[Test]
		public void Set_force_replication_after_formalization()
		{
			var client = TestClient.CreateNaked();
			client.Settings.BuyingMatrix = session.Load<TestMatrix>(localPrice.Matrix.Id);
			session.CreateSQLQuery("insert into Usersettings.AnalitFReplicationInfo(UserId, FirmCode, ForceReplication) " +
				"select :userId, pd.FirmCode, 0 from Customers.Intersection i " +
				"	join Usersettings.Pricesdata pd on pd.PriceCode = i.PriceId " +
				"where i.ClientId = :clientId " +
				"group by i.ClientId, pd.FirmCode")
				.SetParameter("clientId", client.Id)
				.SetParameter("userId", client.Users[0].Id)
				.ExecuteUpdate();

			FormalizeDefaultData();

			var replications = session
				.CreateSQLQuery("select FirmCode, ForceReplication from Usersettings.AnalitFReplicationInfo where UserId = :userId")
				.SetParameter("userId", client.Users[0].Id)
				.List<object[]>();
			Assert.That(replications.Count, Is.GreaterThan(0));
			foreach (var replication in replications)
				Assert.That(replication[1], Is.EqualTo(1), replication[0].ToString());
		}
	}
}