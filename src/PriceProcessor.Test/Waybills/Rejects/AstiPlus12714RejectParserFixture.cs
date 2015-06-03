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
	class AstiPlus12714RejectParserFixture : BaseRejectFixture
	{
		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// </summary>
		[Test]
		public void Parse()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38777832_АСТИ плюс(000186324).dbf");
			var parser = new AstiPlus12714RejectParser();
			var reject = parser.CreateReject(log);
			
			//Проверяем правильность парсинга

			//В файле 3 строки - одна неправильная, соответственно в отказе должно быть 2 строки
			Assert.That(reject.Lines.Count, Is.EqualTo(19));
			//Ну и проверим, что та плохая строка также отмечена
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Аджисепт таб д/рассасывания №24 - апельс"));
			Assert.That(line.Producer, Is.EqualTo("Аджио Фармацевтикалз Лтд"));
			Assert.That(line.Rejected, Is.EqualTo(2));
			Assert.That(line.Ordered, Is.EqualTo(0));
			Assert.That(line.Cost, Is.EqualTo(0));
		}
	}
}
