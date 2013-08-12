using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Waybills;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	class DetectWrongXmlFixture : DocumentFixture
	{
		[Test]
		public void ParseWithoutDate()
		{
			var file = "WrongXmlData1.xml";
			var log = CreateTestLog(file);

			var service = new WaybillService();
			var ids = service.ParseWaybill(new[] { log.Id });
			Assert.That(ids.Count(), Is.EqualTo(1));
		}

		[Test]
		public void ParseWithoutPositions()
		{
			var file = "WrongXmlData.xml";
			var log = CreateTestLog(file);

			var service = new WaybillService();
			var ids = service.ParseWaybill(new[] { log.Id });
			Assert.That(ids.Count(), Is.EqualTo(0));
		}

		[Test]
		public void ParseWithoutProduct()
		{
			var file = "WrongXmlData2.xml";
			var log = CreateTestLog(file);

			var service = new WaybillService();
			var ids = service.ParseWaybill(new[] { log.Id });
			Assert.That(ids.Count(), Is.EqualTo(0));
		}
	}
}
