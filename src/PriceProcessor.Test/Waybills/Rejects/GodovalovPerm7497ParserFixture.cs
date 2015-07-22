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
	class GodovalovPerm7497RejectParserFixture : BaseRejectFixture
	{
		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// Для формата txt первого формата
		/// </summary>
		[Test]
		public void Parse()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38421068_Годовалов-Пермь(m20505-Q4108).txt");
			var parser = new GodovalovPerm7497RejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга			
			Assert.That(reject.Lines.Count, Is.EqualTo(1));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Аура прокладки д/груди д/кормящих мам Со"));
			Assert.That(line.Code, Is.EqualTo("613823251"));
			Assert.That(line.Ordered, Is.EqualTo(5));
			Assert.That(line.Rejected, Is.EqualTo(5));
		}

		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// Для формата txt второго формата
		/// </summary>
		[Test]
		public void Parse2()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38404088_Годовалов-Пермь(m20505-Q3302).txt");
			var parser = new GodovalovPerm7497RejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга			
			Assert.That(reject.Lines.Count, Is.EqualTo(4));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Прокладки Либресс Invisible normal N10/Э"));
			Assert.That(line.Ordered, Is.EqualTo(2));
			Assert.That(line.Rejected, Is.EqualTo(1));
		}
	}
}
