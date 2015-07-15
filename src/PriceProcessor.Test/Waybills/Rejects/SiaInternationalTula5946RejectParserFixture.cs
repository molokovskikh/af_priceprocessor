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
	class SiaInternationalTula5946RejectParserFixture : BaseRejectFixture
	{
		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		///  Для формата DBF
		/// </summary>
		[Test]
		public void Parse()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38389164_СИА Интернейшнл-Тула(деф_Р-2506809).DBF");
			var parser = new SiaInternationalTula5946RejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга			
			Assert.That(reject.Lines.Count, Is.EqualTo(2));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Мирамистин 0.01% Р-р местн. прим. 150мл с распылителем"));
			Assert.That(line.Code, Is.EqualTo("86097"));
			Assert.That(line.Ordered, Is.EqualTo(3));
			Assert.That(line.Rejected, Is.EqualTo(3));
		}

		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		///  Для формата DEF
		/// </summary>
		[Test]
		public void Parse2()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38423268_СИА Интернейшнл-Тула(Р-2509171-1).DEF");
			var parser = new SiaInternationalTula5946RejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга			
			Assert.That(reject.Lines.Count, Is.EqualTo(1));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Лизобакт Таб. д/рассасывания Х30"));
			Assert.That(line.Code, Is.EqualTo("92606"));
			Assert.That(line.Ordered, Is.EqualTo(5));
			Assert.That(line.Rejected, Is.EqualTo(5));
		}
	}
}
