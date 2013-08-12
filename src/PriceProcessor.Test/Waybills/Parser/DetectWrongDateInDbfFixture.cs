using System;
using System.Collections.Generic;
using System.Globalization;
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
	class DetectWrongDateInDbfFixture : DocumentFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("WrongDate.dbf");
			var date = document.DocumentDate;
			Assert.Null(date);
		}
	}
}
