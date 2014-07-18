using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.Txt
{
	[TestFixture]
	public class AmarilisParser
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("7814459903-26_00018004.txt");
			var line = doc.Lines[0];
			Assert.AreEqual("АМ000018004", doc.ProviderDocumentId);
			Assert.AreEqual("14.07.2014", doc.DocumentDate.Value.ToShortDateString());
			Assert.AreEqual(5, doc.Lines.Count);
			Assert.AreEqual("07-149", line.Code);
			Assert.AreEqual("Mavala Активный гель для рук Active Hand Gel 150ml 9092201", line.Product);
			Assert.AreEqual("MAVALA ШВЕЙЦАРИЯ", line.Producer);
			Assert.AreEqual(1572, line.Amount);
			Assert.AreEqual(239.8, line.NdsAmount);
			Assert.AreEqual(18, line.Nds);
			Assert.AreEqual(666.1, line.SupplierCostWithoutNDS);
			Assert.AreEqual(786, line.SupplierCost);
			Assert.AreEqual(2, line.Quantity);
		}
	}
}