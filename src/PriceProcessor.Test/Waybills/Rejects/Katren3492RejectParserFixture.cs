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
	class Katren3492RejectParserFixture : BaseRejectFixture
	{
		/// <summary>
		/// Для фотмата TXT
		/// </summary>
		[Test]
		public void Parse()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("kat1018399-otkaz.txt");
			var parser = new Katren3492RejectParser();
			var reject = parser.CreateReject(log);


			Assert.That(reject.Lines.Count, Is.EqualTo(1));
			Assert.That(parser.BadLines.Count, Is.EqualTo(0));

			//Выбираем строку и проверяем правильно ли все распарсилось
			var line = reject.Lines[0];
			Assert.That(line.Product, Is.EqualTo("ПИРАЦЕТАМ 0,4 N60 КАПС /АКРИХИН/"));
			Assert.That(line.Ordered, Is.EqualTo(3));
			Assert.That(line.Rejected, Is.EqualTo(3));
		}
	}
}
