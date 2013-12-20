using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class ASMParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("n1013273-медикал.dbf");
			Assert.AreEqual(53, document.Lines.Count);
			var line = document.Lines[0];
			Assert.AreEqual("1013273", document.ProviderDocumentId);
			Assert.AreEqual("1028517", line.Code);
			Assert.AreEqual("тампоны 60х30мм спирт.стер. софт-зеллин уп №100", line.Product);
			Assert.AreEqual("466", line.CodeCr);
			Assert.AreEqual("пауль хартманн", line.Producer);
			Assert.AreEqual("германия", line.Country);
			Assert.AreEqual(@"10130142/291013/0007665/1", line.BillOfEntryNumber);
			Assert.AreEqual("9999791", line.SerialNumber);
			Assert.AreEqual("РОСС DE.АЕ83.Д13814", line.Certificates);
			Assert.AreEqual("05.08.2015", line.CertificatesDate);
			Assert.AreEqual(10, line.Nds);
			Assert.AreEqual(64.28, line.SupplierCost);
			Assert.AreEqual(321.4, line.Amount);
		}
	}
}