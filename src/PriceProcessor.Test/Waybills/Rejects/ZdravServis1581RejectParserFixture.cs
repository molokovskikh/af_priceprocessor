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
	class ZdravServis1581RejectParserFixture : BaseRejectFixture
	{
		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// </summary>
		[Test]
		public void Parse()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38387060_Здравсервис(refusal_9844046).txt");
			var parser = new ZdravServis1581RejectParser();
			var reject = parser.CreateReject(log);

			Assert.That(reject.Lines.Count, Is.EqualTo(2));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Артрофоон табл. д/рассас. №100"));
			Assert.That(line.Cost, Is.EqualTo(200.7));
			Assert.That(line.Ordered, Is.EqualTo(1));
			Assert.That(line.Rejected, Is.EqualTo(1));
		}


		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// Файл с одним отказом
		/// </summary>
		[Test]
		public void Parse2()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38409598_Здравсервис(refusal_9852945).txt");
			var parser = new ZdravServis1581RejectParser();
			var reject = parser.CreateReject(log);

			Assert.That(reject.Lines.Count, Is.EqualTo(1));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Инокаин(Оксибупрокаин) капли глазн 0.4% 5мл фл-кап ПЭ"));
			Assert.That(line.Cost, Is.EqualTo(103.05));
			Assert.That(line.Ordered, Is.EqualTo(1));
			Assert.That(line.Rejected, Is.EqualTo(1));
		}
	}
}
