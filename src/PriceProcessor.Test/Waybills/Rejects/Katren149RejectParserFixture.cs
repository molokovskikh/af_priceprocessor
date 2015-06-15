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
	class Katren149RejectParserFixture : BaseRejectFixture
	{
		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// Для формата XLS
		/// </summary>
		[Test]
		public void Parse()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("35111806_Катрен(4193652_г. Рославль_ мкр. 15-й_ д.17_otk).xls");
			var parser = new Katren149RejectParser();
			var reject = parser.CreateReject(log);


			Assert.That(reject.Lines.Count, Is.EqualTo(2));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Code, Is.EqualTo("37083455"));
			Assert.That(line.Product, Is.EqualTo("ЭКВАТОР 0,005+0,01 N10 ТАБЛ"));
			Assert.That(line.Ordered, Is.EqualTo(1));
			Assert.That(line.Rejected, Is.EqualTo(1));
		}

		/// <summary>
		/// Для фотмата TXT
		/// </summary>
		[Test]
		public void Parse2()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("35230956_Катрен(4202120_otk).txt");
			var parser = new Katren149RejectParser();
			var reject = parser.CreateReject(log);


			Assert.That(reject.Lines.Count, Is.EqualTo(1));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Code, Is.EqualTo("16504717"));
			Assert.That(line.Product, Is.EqualTo("ДОНОРМИЛ 0,015 N30 ТАБЛ П/О"));
			Assert.That(line.Ordered, Is.EqualTo(10.00));
			Assert.That(line.Rejected, Is.EqualTo(10.00));
		}


		/// <summary>
		/// Для фотмата TXT, где отказов нет
		/// </summary>
		[Test]
		public void Parse3()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("35231798_Катрен(4202207_otk).txt");
			var parser = new Katren149RejectParser();
			var reject = parser.CreateReject(log);

			Assert.That(reject.Lines.Count, Is.EqualTo(0));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));
		}
	}
}
