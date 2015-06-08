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
	class SiaInternational174RejectParserFixture : BaseRejectFixture
	{
		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// </summary>
		[Test]
		public void Parse()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("36688086_Сиа Интернейшнл - Екатеринбург(1009020771_UVED-2741825).csv");
			var parser = new SiaInternational174RejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга

			//В файле 3 строки - одна неправильная, соответственно в отказе должно быть 2 строки
			Assert.That(reject.Lines.Count, Is.EqualTo(2));
			//Ну и проверим, что та плохая строка также отмечена
			Assert.That(parser.BadLines.Count, Is.EqualTo(1));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Code, Is.EqualTo("98"));
			Assert.That(line.Product, Is.EqualTo("Панангин 45.2мг/мл+40мг/мл конц. д/приг. р-ра д/инф. 10мл Амп. Х5 Б М (R)"));
			Assert.That(line.Producer, Is.EqualTo("Гедеон Рихтер (sc)"));
			Assert.That(line.Cost, Is.EqualTo(135.01m));
			Assert.That(line.Ordered, Is.EqualTo(3));
			Assert.That(line.Rejected, Is.EqualTo(3));
		}

		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/35297
		/// Парсер отказа в котором нет отказов
		/// </summary>
		[Test]
		public void Parse2()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("39120212_Сиа Интернейшнл - Екатеринбург(1006002427_UVED-2858687).csv");
			var parser = new SiaInternational174RejectParser();
			var reject = parser.CreateReject(log);
			
			//Проверяем правильность парсинга

			//В файле 3 строки - одна неправильная, соответственно в отказе должно быть 2 строки
			Assert.That(reject.Lines.Count, Is.EqualTo(0));
			//Ну и проверим, что та плохих строк нет
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));
		}
	}
}
