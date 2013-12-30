using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class PureProfitParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse(@"PureProfit.XML");
			Assert.AreEqual("404", doc.ProviderDocumentId);
			Assert.AreEqual("18.05.2012", doc.DocumentDate.Value.ToShortDateString());
			Assert.AreEqual(12, doc.Lines.Count);
			var line = doc.Lines[0];
			Assert.AreEqual("Б0006843", line.Code);
			Assert.AreEqual("4015400122807", line.EAN13);
			Assert.AreEqual("P&G гиг.подгузники PAMPERS  (2) Mini 18 шт Слип энд Плей", line.Product);
			Assert.AreEqual(10, line.Quantity);
			Assert.AreEqual(118.21, line.SupplierCostWithoutNDS);
			Assert.AreEqual(130.03, line.SupplierCost);
			Assert.AreEqual(10, line.Nds);
			Assert.AreEqual(1300.3, line.Amount);
		}
	}
}