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
	class NadezhdaFarm196RejectParserFixture : BaseRejectFixture
	{
		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// </summary>
		[Test]
		public void Parse()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38386380_Надежда-Фарм ГК(protocol).txt");
			var parser = new NadezhdaFarm196RejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем верность парсинга
			Assert.AreEqual(2, reject.Lines.Count);
			var line = reject.Lines[0];
			Assert.AreEqual("Аскорбиновая к-та драже N200", line.Product);
			Assert.AreEqual("Марбиофарм ОА", line.Producer);
			Assert.AreEqual(10, line.Rejected);
			Assert.AreEqual(0, line.Cost);
		}

		[Test]
		public void Parse2()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("38383250_Надежда-Фарм ГК-null(protocol).txt");
			var parser = new NadezhdaFarm196RejectParser();
			var reject = parser.CreateReject(log);

			//Проверяем верность парсинга
			Assert.AreEqual(0, reject.Lines.Count);
		}
	}
}
