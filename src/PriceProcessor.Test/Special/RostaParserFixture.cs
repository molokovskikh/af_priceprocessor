using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Rosta;
using log4net.Config;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using Test.Support;

namespace PriceProcessor.Test.Special
{
	[TestFixture]
	public class RostaParserFixture
	{
		[Test]
		public void Plan_next_update()
		{
			SystemTime.Now = () => DateTime.Parse("11.02.2010 17:19");
			var plan = new Plan(1, 1, 1, "123", "", DateTime.Now);
			plan.PlanNextUpdate();
			Assert.That(plan.PlanedOn, Is.GreaterThan(DateTime.Parse("12.02.2010 6:10")).And.LessThan(DateTime.Parse("12.02.2010 7:40")));

			//если загрузили в пятницу по следующая загрузка в понедельник
			SystemTime.Now = () => DateTime.Parse("12.02.2010 17:19");
			plan = new Plan(1, 1, 1, "123", "", DateTime.Now);
			plan.PlanNextUpdate();
			Assert.That(plan.PlanedOn, Is.GreaterThan(DateTime.Parse("15.02.2010 6:10")).And.LessThan(DateTime.Parse("15.02.2010 7:40")));

			//если priceprocessor остановили и запустили в субботу то грузим а следующая загрузка в подедельник
			SystemTime.Now = () => DateTime.Parse("13.02.2010 17:19");
			plan = new Plan(1, 1, 1, "123", "", DateTime.Now);
			plan.PlanNextUpdate();
			Assert.That(plan.PlanedOn, Is.GreaterThan(DateTime.Parse("15.02.2010 6:10")).And.LessThan(DateTime.Parse("15.02.2010 7:40")));

			SystemTime.Now = () => DateTime.Parse("14.02.2010 17:19");
			plan = new Plan(1, 1, 1, "123", "", DateTime.Now);
			plan.PlanNextUpdate();
			Assert.That(plan.PlanedOn, Is.GreaterThan(DateTime.Parse("15.02.2010 6:10")).And.LessThan(DateTime.Parse("15.02.2010 7:40")));
		}

		[Test]
		public void Read_extended_columns()
		{
			var data = new RostaReader().ReadAdditions(@"..\..\Data\rosta\ex");

			//Супрастин амп.р-р д/ин.  20мг/1мл х 5
			var addition = data[132];
			Assert.That(addition.Id, Is.EqualTo(9061));
			Assert.That(addition.Period, Is.EqualTo(new DateTime(2014, 10, 1)));
			Assert.That(addition.Pack, Is.EqualTo(225));
			Assert.That(addition.RegistryCost, Is.EqualTo(105.509f));
			Assert.That(addition.VitallyImportant, Is.True);
		}

		[Test]
		public void Read_extended_columns_for_kazan()
		{
			var additions = new KazanAdditionReader().ReadAdditions(@"..\..\Data\rosta\ex_kazan");
			var addition = additions.First(a => a.Id == 16718);
			Assert.That(addition.VitallyImportant, Is.True);
			Assert.That(Math.Round(addition.RegistryCost, 2), Is.EqualTo(55.67));
		}

		[Test]
		public void Crypt_hardware_info()
		{
			Assert.That(
				RostaDecoder.CryptHwinfo("0001067A-00020800-0C08E3FD-BFEBFBFF-05B0B101-005657F0-2CB4304E-GenuineIntel\r\n",-830816681),
				Is.EqualTo("E1757DDE426A02EC7FBC276124BB6A9E067141AD7C7E975E23869215B8431D63D817DB356036B966F0F4565AF2747ED4456E04ED63A33B1256BD619F1C185DAB2E29A1060AA5F739851D332E9351CE4A3967BF3ADA730C29BE737CDF3E0F06EB6CB4436358FD479245B0")
			);

			Assert.That(
				RostaDecoder.CryptHwinfo("00000F65-00020800-0000E49D-BFEBFBFF-605B5101-007D7040-GenuineIntel\r\n02/05/2007-I945-6A79TG0AC-00", -972240371),
				Is.EqualTo("EC8078DA4D11169D70B7326C33B66599117A4CD97781985C69899700BF341856D71ADE3E6D3EC30388016157F77F79DB3A610E926EA4433971F134C44611159323DED2605FF99672C95C681EA46BB75C6239C16D8872655C83097AAF23780B955EED4C3822B43EEB454F13A17B")
			);
		}

		[Test]
		public void Decode_rosta_key()
		{
			var data = "EC8078DA4D11169D70B7326C33B66599117A4CD97781985C69899700BF341856D71ADE3E6D3EC30388016157F77F79DB3A610E926EA4433971F134C44611159323DED2605FF99672C95C681EA46BB75C6239C16D8872655C83097AAF23780B955EED4C3822B43EEB454F13A17B";
			var bytes = new byte[data.Length / 2];

			for(var i = 0; i < bytes.Length; i++)
			{
				bytes[i] = Convert.ToByte(data.Substring(i * 2, 2), 16);
			}

			for(var i = 0; i < bytes.Length; i++)
			{
				if (i != bytes.Length - 4)
				{
					bytes[i] ^= bytes[bytes.Length - 4];
				}
			}

			for(var i = 0; i < bytes.Length; i++)
			{
				bytes[i] ^= (byte) (Math.Round(Math.Sin(i)*128, 0) - bytes.Length);
			}
			Console.WriteLine(Encoding.ASCII.GetString(bytes));
		}

		[Test]
		public void Formilize_price()
		{
		    TestPrice price;
		    using (new TransactionScope())
		    {
                price = RostaFixture.CreatePriceForRosta();    
		    }
			
			var priceItemId = price.Costs[0].PriceItem.Id;
			using (var connection = new MySqlConnection(Literals.ConnectionString()))
			{
				var data = PricesValidator.LoadFormRules(priceItemId);
				var parser = new FakeRostaParser(@"..\..\Data\Rosta\price",
					@"..\..\Data\Rosta\producers",
					@"..\..\Data\Rosta\ex",
					connection, 
					data);
				connection.Close();
				parser.AdditionReader = new RostaReader();
				parser.Formalize();
				if (connection.State == ConnectionState.Closed)
					connection.Open();
				var command = new MySqlCommand("update usersettings.priceitems set PriceDate = now() where id = ?id", connection);
				command.Parameters.AddWithValue("?id", priceItemId);
				command.ExecuteNonQuery();
			}
			using (new SessionScope())
				Assert.That(TestCore.Queryable.Where(c => c.Price == price).Count(), Is.GreaterThan(0));
		}

		[Test, Ignore("Для тестирования руками, часто не запускать что бы не спалиться")]
		public void Download_price()
		{
			if (Directory.Exists("output"))
				Directory.Delete("output", true);

			Directory.CreateDirectory("output");
			var downloader = new RostaDownloader();
			downloader.DownloadPrice("20070122094213-218-44", "00000F49-00010800-0000651D-BFEBFBFF-605B5101-003C7040-GenuineIntel\r\n12/21/2005-Grantsdale-P5GV-TMX-00", "price", "producers", "ex");
		}
	}
}
