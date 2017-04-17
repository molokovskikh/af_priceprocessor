using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class BelaLtdParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("13916.dbf", new DocumentReceiveLog(new Supplier { Id = 2109 }, new Address(new Client())));
			Assert.AreEqual("13916", doc.ProviderDocumentId);
			Assert.AreEqual("08.08.2014", doc.DocumentDate.Value.ToShortDateString());
			Assert.AreEqual(2, doc.Lines.Count);
			var line = doc.Lines[0];
			Assert.AreEqual("Гигиенические пеленки Сени софт 90х60см 30шт кор.30х2", line.Product);
			Assert.AreEqual(1, line.Quantity);
			Assert.AreEqual(558.01, line.SupplierCostWithoutNDS);
			Assert.AreEqual(552.48, line.ProducerCostWithoutNDS);
			Assert.AreEqual("TZMO SA", line.Producer);
			Assert.AreEqual(613.81, line.SupplierCost);
			Assert.AreEqual(55.80, line.NdsAmount);
			Assert.AreEqual(10, line.Nds);
			Assert.AreEqual(5900516691295, line.EAN13);
		}
	}
}