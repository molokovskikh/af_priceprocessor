using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class UgFarmParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("12282-1_2401382.dbf", new DocumentReceiveLog(new Supplier {
				Id = 13717
			}, new Address(new Client())));
			//в SUMPAY передается сумма по строке
			Assert.AreEqual(5316.19m, doc?.Invoice?.Amount);
			var line = doc.Lines[0];
			Assert.AreEqual("Блемарен таб №80 раств шип", line.Product);
			Assert.AreEqual("Эспарма ГмбХ - Германия", line.Producer);
			Assert.AreEqual(847.6, line.ProducerCostWithoutNDS);
			Assert.AreEqual(854.8, line.SupplierCostWithoutNDS);
			Assert.AreEqual(940.28, line.SupplierCost);
			Assert.AreEqual(2, line.Quantity);
		}
	}
}