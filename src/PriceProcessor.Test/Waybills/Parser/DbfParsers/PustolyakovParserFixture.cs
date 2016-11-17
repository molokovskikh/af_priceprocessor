using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class PustolyakovParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("00041962.DBF");
			Assert.That(doc.Parser, Is.EqualTo("PustolyakovParser"));
			Assert.AreEqual(doc.ProviderDocumentId, "А0000041962");
			Assert.AreEqual(doc.DocumentDate.Value.ToShortDateString(), "29.10.2016");
			var line = doc.Lines[0];
			Assert.AreEqual(line.Code, "Н0000153129");
			Assert.AreEqual(line.Product, "Garn Fructis шамп.SOS Восстан-е 400мл(150 в подарок)");
			Assert.AreEqual(line.Quantity, 1);
			Assert.AreEqual(line.Nds, 18);
			Assert.AreEqual(line.NdsAmount, 19.62);
			Assert.AreEqual(line.SupplierCost, 128.59);
			Assert.AreEqual(line.Amount, 128.59);
			Assert.IsNull(line.VitallyImportant);
			Assert.IsNull(line.RegistryCost);
			Assert.AreEqual(line.Certificates, "RU Д-RU.ПК05.В.04024");
			Assert.IsNull(line.Period);
			Assert.AreEqual(line.Producer, "Л Ореаль");
			Assert.AreEqual(line.Country, "РОССИЯ");
			Assert.AreEqual(line.EAN13, "4690214107426");
		}

	}
}
