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
	class FarmkomplektVoroneth4365RejectParserFixture : BaseRejectFixture
	{
		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// Для формата txt
		/// </summary>
		[Test]
		public void Parse()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38382962_Фармкомплект-Воронеж(Отказ по заявке Аптека).txt");
			var parser = new FarmkomplektVoronezh4365RejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга			
			Assert.That(reject.Lines.Count, Is.EqualTo(1));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Супрадин №30 таб. п/о  Dragenopharm"));
			Assert.That(line.Ordered, Is.EqualTo(2));
			Assert.That(line.Rejected, Is.EqualTo(2));
		}

		/// <summary>
		/// Для другого вида файла txt
		/// </summary>
		[Test]
		public void Parse2()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("40287816_Фармкомплект-Воронеж(Отказ по заявке Аптека 3 ул. Садовая).txt");
			var parser = new FarmkomplektVoronezh4365RejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга			
			Assert.That(reject.Lines.Count, Is.EqualTo(1));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Фромилид уно 500мг №14 таб.пролонг.д-я п/о  KRKA"));
			Assert.That(line.Ordered, Is.EqualTo(1));
			Assert.That(line.Rejected, Is.EqualTo(1));
		}

		/// <summary>
		/// Для другого вида файла txt
		/// </summary>
		[Test]
		public void Parse3()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38390094_Фармкомплект-Воронеж(Отказ по заявке Фармакор).txt");
			var parser = new FarmkomplektVoronezh4365RejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга			
			Assert.That(reject.Lines.Count, Is.EqualTo(1));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Виферон-1 150тыс. МЕ №10 супп.рект.  Ферон"));
			Assert.That(line.Ordered, Is.EqualTo(3));
			Assert.That(line.Rejected, Is.EqualTo(3));
		}

		/// <summary>
		/// Для другого вида файла txt
		/// </summary>
		[Test]
		public void Parse4()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38458654_Фармкомплект-Воронеж(Отказ по заявке Аптека).txt");
			var parser = new FarmkomplektVoronezh4365RejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга			
			Assert.That(reject.Lines.Count, Is.EqualTo(1));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Цефотаксим 1г пор. д/ин в/в и в/м фл.  Красфарма"));
			Assert.That(line.Ordered, Is.EqualTo(20));
			Assert.That(line.Rejected, Is.EqualTo(20));
		}

		/// <summary>
		/// Для формата TXT с опечаткой в строке отказа
		/// </summary>
		[Test]
		public void Parse5()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38407196_Фармкомплект-Воронеж(Отказ по заявке Фармакор 179 Курск_пр-т Дружбы 7).txt");
			var parser = new FarmkomplektVoronezh4365RejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга			
			Assert.That(reject.Lines.Count, Is.EqualTo(0));
			Assert.That(parser.BadLines.Count, Is.EqualTo(1));
		}
	}
}
