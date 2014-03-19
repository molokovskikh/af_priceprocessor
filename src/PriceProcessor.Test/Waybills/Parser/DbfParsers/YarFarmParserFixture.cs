using System;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class YarFarmParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("36597_МР0311-1497-1.dbf");
			Assert.AreEqual(new DateTime(2014, 3, 12), doc.DocumentDate);
			Assert.AreEqual("МР0311-1497-1", doc.ProviderDocumentId);
			Assert.AreEqual(19, doc.Lines.Count);
			var line = doc.Lines[0];
			Assert.AreEqual("Бахилы полиэтиленовые в инд. уп. №2 (China MEHECO Medical Instruments)", line.Product);
			Assert.AreEqual(@"China MEHECO Medical Instruments/X. X. Pr.\Ammex Weida)", line.Producer);
			Assert.AreEqual(100, line.Quantity);
			Assert.AreEqual(10, line.Nds);
			Assert.AreEqual(1.7, line.SupplierCostWithoutNDS);
			Assert.AreEqual(1.87, line.SupplierCost);
		}
	}
}