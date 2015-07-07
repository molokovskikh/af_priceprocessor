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
	class Yarfarma14960RejectParserFixture : BaseRejectFixture
	{
		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// </summary>
		[Test]
		public void Parse()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38495282_Ярфарма(42985_000000127444520150507181334).txt");
			var parser = new Yarfarma14960RejectParser();
			var reject = parser.CreateReject(log);

			Assert.That(reject.Lines.Count, Is.EqualTo(1));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Масло ванили 10мл (Лекус)"));
			Assert.That(line.Ordered, Is.EqualTo(1));
			Assert.That(line.Rejected, Is.EqualTo(1));
		}


		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// Файл с одним отказом из некорректно заполненных строк по отказам(где в количестве заказанных товаров стоит ноль)
		/// </summary>
		[Test]
		public void Parse2()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38387666_Ярфарма(43171_000000126489520150504160849).txt");
			var parser = new Yarfarma14960RejectParser();
			var reject = parser.CreateReject(log);

			Assert.That(reject.Lines.Count, Is.EqualTo(1));
			Assert.That(parser.BadLines.Count, Is.EqualTo(28));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Йода Раствор спиртовой 5% Флакон 10мл (Гиппократ ООО)"));
			Assert.That(line.Ordered, Is.EqualTo(20));
			Assert.That(line.Rejected, Is.EqualTo(20));
		}
	}
}
