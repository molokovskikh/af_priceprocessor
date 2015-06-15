using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Rejects.Parser;
using NHibernate.Linq;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using PriceProcessor.Test.Waybills.Rejects.Infrastructure;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills.Rejects
{
	[TestFixture]
	class PulsBryansk7975RejectParserFixture : BaseRejectFixture
	{
		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// Для CSV файла
		/// </summary>
		[Test]
		public void Parse()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38382390_ПУЛЬС Брянск(def_70664427).csv");
			var parser = new PulsBryansk7975RejectParser();
			var reject = parser.CreateReject(log);
			
			//Проверяем правильность парсинга

			Assert.That(reject.Lines.Count, Is.EqualTo(1));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Code, Is.EqualTo("20469"));
			Assert.That(line.Product, Is.EqualTo("Акридерм ГК мазь туба 15 г. х1"));
			Assert.That(line.Cost, Is.EqualTo(409.97));
			Assert.That(line.Ordered, Is.EqualTo(3));
			Assert.That(line.Rejected, Is.EqualTo(1));
		}

		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// Для TXT файла
		/// </summary>
		[Test]
		public void Parse2()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38377390_ПУЛЬС Брянск(def_70657293).txt");
			var parser = new PulsBryansk7975RejectParser();
			  var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга

			Assert.That(reject.Lines.Count, Is.EqualTo(1));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Code, Is.EqualTo("13257"));
			Assert.That(line.Product, Is.EqualTo("Проктозан мазь 20 г. х1"));
			Assert.That(line.Cost, Is.EqualTo(0m));
			Assert.That(line.Ordered, Is.EqualTo(1));
			Assert.That(line.Rejected, Is.EqualTo(1));
		}	
	}
}
