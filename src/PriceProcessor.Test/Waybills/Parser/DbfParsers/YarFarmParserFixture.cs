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

		[Test]
		public void Parse_alpha_medica()
		{
			var doc = WaybillParser.Parse(@"C:\Users\kvasov\Downloads\b036504.dbf");
			Assert.AreEqual(1, doc.Lines.Count);
			var line = doc.Lines[0];
			Assert.AreEqual("612", line.Code);
			Assert.AreEqual("Изм. арт. давл. WA-55 автомат, 3Check, аритмия, манжета М-L, адаптер в компл.", line.Product);
			Assert.AreEqual("B.Well", line.Producer);
			Assert.AreEqual("КИТАЙ", line.Country);
			Assert.AreEqual(1362, line.SupplierCost);
			Assert.AreEqual(0, line.Nds);
			Assert.AreEqual("РОСС GB.ИМ04.Д01219", line.Certificates);
			Assert.AreEqual("10130090/181013/0083963/1", line.BillOfEntryNumber);
			Assert.AreEqual(1, line.Quantity);
		}
	}
}