using System;
using System.ComponentModel;
using System.Runtime.ConstrainedExecution;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class ABGroupParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("19022014.DBF");
			var line = document.Lines[0];
			Assert.AreEqual("ААША VEDA VEDIСA Бальзам СОФТ от трещин питательный 20 гр", line.Product);
			Assert.AreEqual(68, line.SupplierCost);
			Assert.AreEqual(57.63, line.SupplierCostWithoutNDS);
			Assert.AreEqual(1, line.Quantity);
		}
	}
}