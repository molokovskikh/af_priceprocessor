using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Rejects.Parser;
using NHibernate.Linq;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using PriceProcessor.Test.Waybills.Rejects.Infrastructure;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills.Rejects
{
	[TestFixture]
	class NadezhdaFarm7579RejectParserFixture : BaseRejectFixture
	{
		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// </summary>
		[Test]
		public void Parse()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("35115498_Надежда-Фарм Орел_Фарма Орел(protocol).txt");
			var reject = new NadezhdaFarm7579RejectParser().CreateReject(log);
			
			//Проверяем верность парсинга
			Assert.AreEqual(1, reject.Lines.Count);
			var line = reject.Lines[0];
			Assert.AreEqual("Юниэнзим с МПС таб п/о N20", line.Product);
			Assert.AreEqual("Юникем Лабора", line.Producer);
			Assert.AreEqual(3, line.Rejected);
			Assert.AreEqual(0, line.Cost);
		}
	}
}
