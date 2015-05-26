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
	class SiaInternational174RejectParserFixture : BaseRejectFixture
	{
		/// <summary>
		/// Тест к задаче http://redmine.analit.net/issues/33351
		/// </summary>
		[Test]
		public void Parse()
		{
			//Создаем лог, а затем отказ
			var log = CreateRejectLog("36688086_Сиа Интернейшнл - Екатеринбург(1009020771_UVED-2741825).csv");
			var reject = new SiaInternational174RejectParser().CreateReject(log);
			
			//Проверяем правильность парсинга
			Assert.AreEqual(1, reject.Lines.Count);
			var line = reject.Lines[0];
			Assert.That(line.Code, Is.EqualTo("98"));
			Assert.That(line.Product, Is.EqualTo("Панангин 45.2мг/мл+40мг/мл конц. д/приг. р-ра д/инф. 10мл Амп. Х5 Б М (R)"));
			Assert.That(line.Producer, Is.EqualTo("Гедеон Рихтер (sc)"));
			Assert.That(line.Cost, Is.EqualTo(135.01m));
			Assert.That(line.Ordered, Is.EqualTo(3));
			Assert.That(line.Rejected, Is.EqualTo(3));
		}
	}
}
