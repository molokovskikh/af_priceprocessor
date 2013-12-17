using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class FarmCenterFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("D0046395.dbf");
			Assert.AreEqual(1, document.Lines.Count);
			var line = document.Lines[0];
			Assert.AreEqual("Анальгин  500мг №10 таб", line.Product);
			Assert.AreEqual("Асфарма ОАО Россия", line.Producer);
			Assert.AreEqual(50701526, line.OrderId);
		}
	}
}