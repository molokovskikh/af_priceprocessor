using System;
using System.IO;
using Common.Tools;
using Inforoom.PriceProcessor.Rosta;
using NUnit.Framework;

namespace PriceProcessor.Test.Special
{
	[TestFixture]
	public class RostaParserFixture
	{
		[Test]
		public void Plan_next_update()
		{
			SystemTime.Now = () => DateTime.Parse("11.02.2010 17:19");
			var plan = new Plan(1, "123");
			plan.PlanNextUpdate();
			Assert.That(plan.PlanedOn, Is.GreaterThan(DateTime.Parse("12.02.2010 6:10")).And.LessThan(DateTime.Parse("12.02.2010 7:40")));

			//если загрузили в пятницу по следующая загрузка в понедельник
			SystemTime.Now = () => DateTime.Parse("12.02.2010 17:19");
			plan = new Plan(1, "123");
			plan.PlanNextUpdate();
			Assert.That(plan.PlanedOn, Is.GreaterThan(DateTime.Parse("15.02.2010 6:10")).And.LessThan(DateTime.Parse("15.02.2010 7:40")));

			//если priceprocessor остановили и запустили в субботу то грузим а следующая загрузка в подедельник
			SystemTime.Now = () => DateTime.Parse("13.02.2010 17:19");
			plan = new Plan(1, "123");
			plan.PlanNextUpdate();
			Assert.That(plan.PlanedOn, Is.GreaterThan(DateTime.Parse("15.02.2010 6:10")).And.LessThan(DateTime.Parse("15.02.2010 7:40")));

			SystemTime.Now = () => DateTime.Parse("14.02.2010 17:19");
			plan = new Plan(1, "123");
			plan.PlanNextUpdate();
			Assert.That(plan.PlanedOn, Is.GreaterThan(DateTime.Parse("15.02.2010 6:10")).And.LessThan(DateTime.Parse("15.02.2010 7:40")));
		}

		[Test]
		public void Read_extended_columns()
		{
			var data = RostaReader.ReadAddtions(@"..\..\Data\rosta\ex");
			var addition = data[0];
			Assert.That(addition.Period, Is.EqualTo(new DateTime(2013, 10, 07)));
			Assert.That(addition.Id, Is.EqualTo(8204));
			addition = data[1];
			Assert.That(addition.Period, Is.EqualTo(new DateTime(2011, 06, 22)));
		}

		[Test, Ignore("Для тестирования руками, часто не запускать что бы не спалиться")]
		public void Download_price()
		{
			if (Directory.Exists("output"))
				Directory.Delete("output", true);

			Directory.CreateDirectory("output");
			var downloader = new RostaDownloader();
			downloader.DownloadPrice("20100120154920-157-12", "price", "producers", "ex");
		}

	}
}
