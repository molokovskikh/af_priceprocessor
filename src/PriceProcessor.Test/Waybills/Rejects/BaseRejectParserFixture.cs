using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Rejects.Parser;
using NUnit.Framework;
using PriceProcessor.Test.Waybills.Rejects.Infrastructure;

namespace PriceProcessor.Test.Waybills.Rejects
{
	[TestFixture]
	class BaseRejectParserFixture : BaseRejectFixture
	{
		[Test(Description = "Проверка того, что плохие линии не читаются")]
		public void SkipLines()
		{
			//Мы знаем, что в логе 3 строки: из них должна быть одна невалидная
			var log = CreateRejectLog("36688086_Сиа Интернейшнл - Екатеринбург(1009020771_UVED-2741825).csv");
			var parser = new SiaInternational174RejectParser();
			var reject = parser.CreateReject(log);
			Assert.That(parser.BadLines.Count, Is.EqualTo(1));
			Assert.That(reject.Lines.Count, Is.EqualTo(2));
		}
	}
}
