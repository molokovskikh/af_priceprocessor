using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class ProfitmedParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("ПрофитмедСПб_791_434_14.dbf");
			Assert.AreEqual("791/434-14", doc.ProviderDocumentId);
			Assert.AreEqual("03.07.2014", doc.DocumentDate.Value.ToShortDateString());
			var line = doc.Lines[0];
			Assert.AreEqual("Аскорбиновая кислота таб жеват 25мг N10 бум параф (крутка)", line.Product);
			Assert.AreEqual("Марбиофарм ОАО", line.Producer);
			Assert.AreEqual(5.5, line.SupplierCost);
			Assert.AreEqual(10, line.Nds);
		}
	}
}