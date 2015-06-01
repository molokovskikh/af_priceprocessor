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
	class AlianceHelskea3027RejectParserFixture : BaseRejectFixture
	{
		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// </summary>
		[Test(Description = "Проверка оригинального файла")]
		public void Parse()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("35150830_Альянс Хелскеа Рус(RefusalReport).xls");
			var parser = new AllianceHelskea3027RejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга			
			Assert.That(reject.Lines.Count, Is.EqualTo(1));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Ромашки цветки фильтр-пакеты 1.5г N20 Россия"));
			Assert.That(line.Code, Is.EqualTo("36324"));
			Assert.That(line.Producer, Is.EqualTo("Иван-Чай ЗАО"));
			Assert.That(line.Ordered, Is.EqualTo(50));
			Assert.That(line.Rejected, Is.EqualTo(14));
		}

		[Test(Description = "Проверка файла с большим количеством строк")]
		public void Parse2()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("35150830_Альянс Хелскеа Рус2(RefusalReport).xls");
			var parser = new AllianceHelskea3027RejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга			
			Assert.That(reject.Lines.Count, Is.EqualTo(2));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Ромашки цветки фильтр-пакеты 1.5г N20 Россия"));
			Assert.That(line.Code, Is.EqualTo("36324"));
			Assert.That(line.Producer, Is.EqualTo("Иван-Чай ЗАО"));
			Assert.That(line.Ordered, Is.EqualTo(50));
			Assert.That(line.Rejected, Is.EqualTo(14));
		}

		[Test(Description = "Проверка файла в котором нет отказов")]
		public void Parse3()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("35145270_Альянс Хелскеа Рус-null(RefusalReport).xls");
			var parser = new AllianceHelskea3027RejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга			
			Assert.That(reject.Lines.Count, Is.EqualTo(0));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));
		}
	}
}
