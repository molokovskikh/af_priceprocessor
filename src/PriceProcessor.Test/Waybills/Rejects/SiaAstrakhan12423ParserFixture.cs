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
	class SiaAstrakhan12423RejectParserFixture : BaseRejectFixture
	{
		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// Для формата txt с несколькими отказами
		/// </summary>
		[Test]
		public void Parse()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("36222734_Сиа-Астрахань(P-1509223-1).txt");
			var parser = new SiaAstrakhan12423RejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга			
			Assert.That(reject.Lines.Count, Is.EqualTo(2));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Пластырь Сопелка д/ингаляций Х10"));
			Assert.That(line.Ordered, Is.EqualTo(4));
			Assert.That(line.Rejected, Is.EqualTo(4));
		}

		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// Для формата txt с одним отказом
		/// </summary>
		[Test]
		public void Parse2()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38385360_Сиа-Астрахань(P-1566967-1).txt");
			var parser = new SiaAstrakhan12423RejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга			
			Assert.That(reject.Lines.Count, Is.EqualTo(1));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Ко-Эксфорж 5мг+160мг+12.5мг Таб."));
			Assert.That(line.Ordered, Is.EqualTo(2));
			Assert.That(line.Rejected, Is.EqualTo(2));
		}
	}
}
