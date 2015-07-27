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
	class KatrenKazan2754RejectParserFixture : BaseRejectFixture
	{
		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// Для формата TXT
		/// </summary>
		[Test]
		public void Parse()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38417780_Катрен(3293854).txt");
			var parser = new KatrenKazan2754RejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга			
			Assert.That(reject.Lines.Count, Is.EqualTo(1));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("ГЕЛЬ П/ЗАГАРА УСПОКАИВАЮЩ/УВЛАЖНЯЮЩ 125МЛ|Фреш Стайл Косметикс,ООО"));
			Assert.That(line.Code, Is.EqualTo("24080234"));
			Assert.That(line.Ordered, Is.EqualTo(2));
			Assert.That(line.Rejected, Is.EqualTo(2));
		}

		/// <summary>
		/// Для формата OTK
		/// </summary>
		[Test]
		public void Parse2()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38419406_Катрен(3294050).otk");
			var parser = new KatrenKazan2754RejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга			
			Assert.That(reject.Lines.Count, Is.EqualTo(1));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("МЕЗИМ-ФОРТЕ N80 ТАБЛ П/О|Берлин-Хеми АГ/ Берлин-Фарма, ЗАО"));
			Assert.That(line.Code, Is.EqualTo("58728672"));
			Assert.That(line.Ordered, Is.EqualTo(1));
			Assert.That(line.Rejected, Is.EqualTo(1));
		}
	}
}
