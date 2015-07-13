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
	class PulsVoronezh15365RejectParserFixture : BaseRejectFixture
	{
		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// Для CSV файла
		/// </summary>
		[Test]
		public void Parse()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38822674_ПУЛЬС Воронеж(def_71_339_286).csv");
			var parser = new PulsVoronezh15365RejectParser();
			var reject = parser.CreateReject(log);
			
			//Проверяем правильность парсинга
			Assert.That(reject.Lines.Count, Is.EqualTo(1));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Code, Is.EqualTo("12100"));
			Assert.That(line.Product, Is.EqualTo("Ципрофлоксацин табл. п/о плен 500 мг х10"));
			Assert.That(line.Cost, Is.EqualTo(0.00));
			Assert.That(line.Ordered, Is.EqualTo(2));
			Assert.That(line.Rejected, Is.EqualTo(2));
		}

		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// Для TXT файла
		/// </summary>
		[Test]
		public void Parse2()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38423934_ПУЛЬС Воронеж(def_70_747_955).txt");
			var parser = new PulsVoronezh15365RejectParser();
			  var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга
			Assert.That(reject.Lines.Count, Is.EqualTo(5));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Code, Is.EqualTo("21091"));
			Assert.That(line.Product, Is.EqualTo("Боботик капли внутр.пр фл. 30 мл. х1"));
			Assert.That(line.Cost, Is.EqualTo(0m));
			Assert.That(line.Ordered, Is.EqualTo(2));
			Assert.That(line.Rejected, Is.EqualTo(2));
		}
	}
}
