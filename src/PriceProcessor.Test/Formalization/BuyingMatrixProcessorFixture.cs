using System;
using System.Data;
using System.Linq;
using Common.MySql;
using Common.Tools;
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

			var count = MatrixItems().Rows.Count;
			Assert.That(count, Is.GreaterThan(0));
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

			var count = MatrixItems().Rows.Count;
			Assert.That(count, Is.EqualTo(1), "код прайс листа {0}", price.Id);
		}

		private DataTable MatrixItems()
		{
			return With.Connection(c => {
				var adapter = new MySqlDataAdapter("select * from farm.BuyingMatrix where PriceId = ?Priceid", c);
				adapter.SelectCommand.Parameters.AddWithValue("?PriceId", price.Id);
				var table = new DataTable();
				adapter.Fill(table);
				return table;
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

		[Test]
		public void Update_filter_price_on_okp_source_price()
		{
			var origin = price;
			price.CreateAssortmentBoundSynonyms(
				"9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ",
				"Валента Фармацевтика/Королев Ф");
			var core = new TestCore(price.ProductSynonyms[0], price.ProducerSynonyms[0]) {
				CodeOKP = 931201
			};
			session.Save(core);

			var okpPrice = new TestPrice(price.Supplier);
			var format = Configure(okpPrice);
			format.FCodeOkp = "F5";
			session.Save(okpPrice);

			var localOkpPrice = session.Load<Price>(okpPrice.Id);
			localPrice.CodeOkpFilterPrice  = localOkpPrice;
			session.Save(localPrice);

			price = okpPrice;
			CreateDefaultSynonym();
			Formalize(@"9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ;Валента Фармацевтика/Королев Ф;2864;220.92;931201;");

			price = origin;
			var count = MatrixItems().Rows.Count;
			Assert.That(count, Is.EqualTo(1));
		}

		[Test]
		public void Unknown_producers_should_be_exclude_from_matrxi()
		{
			price.AddProductSynonym("911 ВЕНОЛГОН ГЕЛЬ Д/ НОГ ПРИ ТЯЖЕСТИ БОЛИ И ОТЕКАХ ТУБА 100МЛ");
			price.CreateAssortmentBoundSynonyms(
				"5 ДНЕЙ ВАННА Д/НОГ СМЯГЧАЮЩАЯ №10 ПАК. 25Г",
				"Санкт-Петербургская ф.ф.");
			price.CreateAssortmentBoundSynonyms(
				"9 МЕСЯЦЕВ КРЕМ Д/ПРОФИЛАКТИКИ И КОРРЕКЦИИ РАСТЯЖЕК 150МЛ",
				"Валента Фармацевтика/Королев Ф");
			Formalize(defaultContent);

			var items = MatrixItems();
			Assert.That(items.Rows.Count, Is.EqualTo(1));
		}
	}
}