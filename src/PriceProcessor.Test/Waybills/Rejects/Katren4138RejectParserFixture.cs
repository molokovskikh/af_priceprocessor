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
	class Katren4138RejectParserFixture : BaseRejectFixture
	{
		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// </summary>
		[Test]
		public void Parse()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("35151788_Катрен(2370347).xls");
			var parser = new Katren4138RejectParser();
			var reject = parser.CreateReject(log);

			Assert.That(reject.Lines.Count, Is.EqualTo(2));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Code, Is.EqualTo("32686066"));
			Assert.That(line.Product, Is.EqualTo("ЛОШАДИНАЯ СИЛА БАЛЬЗАМ-ГЕЛЬ Д/ТЕЛА 500МЛ"));
			Assert.That(line.Producer, Is.EqualTo("Р. Косметик ООО"));
			Assert.That(line.Ordered, Is.EqualTo(4));
			Assert.That(line.Rejected, Is.EqualTo(2));
		}
	}
}
