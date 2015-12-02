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
	class UniversalRejectParserFixture : BaseRejectFixture
	{
		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// </summary>
		[Test]
		public void Parse()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("refusal_77937852.xml");
			var parser = new UniversalRejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем правильность парсинга			
			Assert.That(reject.Lines.Count, Is.EqualTo(2));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Церебролизин 2мл №10 амп."));
			Assert.That(line.Code, Is.EqualTo("8748518"));
			Assert.That(line.Ordered, Is.EqualTo(10));
			Assert.That(line.Rejected, Is.EqualTo(1));
			Assert.That(line.OrderId, Is.EqualTo(894954));
			Assert.That(line.CodeCr, Is.EqualTo("5841"));
		}
	}
}
