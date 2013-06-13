using System;
using System.Collections.Generic;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using PriceProcessor.Test.Waybills.Parser;
using Test.Support;
using Test.Support.Suppliers;
using Test.Support.log4net;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class WaybillFormatDetectorFixture
	{
		private WaybillFormatDetector detector;

		[SetUp]
		public void Setup()
		{
			detector = new WaybillFormatDetector();
		}

		[Test]
		public void Parse_waybills_with_fixed_format()
		{
			var file = @"C:\Р-1690677.DBF";
			var log = new DocumentReceiveLog(new Supplier { Id = 21 }, new Address { Client = new Client() });
			var doc = WaybillParser.Parse(file, log);
			Assert.IsNotNull(doc);
		}
	}
}