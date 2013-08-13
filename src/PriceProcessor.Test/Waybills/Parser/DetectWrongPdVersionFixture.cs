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
	class DetectWrongPdVersionFixture : DocumentFixture
	{
		[Test]
		public void ParseAnotherVersion()
		{
			var file = "1008fo1.pd";
			var log = CreateTestLog(file);

			var service = new WaybillService();
			var ids = service.ParseWaybill(new[] { log.Id });
			Assert.That(ids.Count(), Is.EqualTo(0));
		}

		[Test]
		public void ParseWithoutData()
		{
			var file = "test_wrong_pd.pd";
			var log = CreateTestLog(file);

			var service = new WaybillService();
			var ids = service.ParseWaybill(new[] { log.Id });
			Assert.That(ids.Count(), Is.EqualTo(0));
		}
	}
}
