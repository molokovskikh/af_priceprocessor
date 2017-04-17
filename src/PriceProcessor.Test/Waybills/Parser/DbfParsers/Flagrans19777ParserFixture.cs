using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class Flagrans19777ParserFixture
	{
		[Test]
		public void ParseFlagrans19777()
		{
			var doc = WaybillParser.Parse("19777.DBF");
			Assert.AreEqual(doc.ProviderDocumentId, "ФЛА00001230");
			Assert.AreEqual(doc.DocumentDate.Value.ToShortDateString(), "03.11.2016");
			var line = doc.Lines[0];
			Assert.AreEqual(line.Code, "3312");
			Assert.AreEqual(line.Product, "Крем - гель для проблемной кожи. Матирующий эффект  50 мл.");
			Assert.AreEqual(line.Quantity, 1);
			Assert.AreEqual(line.Nds, 0);
			Assert.AreEqual(line.NdsAmount, 0.0);
			Assert.AreEqual(line.SupplierCost, 385.0);
			Assert.AreEqual(line.SupplierCostWithoutNDS, 385.0);
			Assert.AreEqual(line.Amount, 385.0);
			Assert.IsNull(line.VitallyImportant);
			Assert.IsNull(line.RegistryCost);
			Assert.AreEqual(line.Certificates, "RU.Д-RU.ПК08.В.00726");
			Assert.IsNull(line.Period);
			Assert.AreEqual(line.Producer, "Россия");
			Assert.AreEqual(line.Country, "Россия");
		}

		[Test]
		public void ParseRossmed7768()
		{
			var doc = WaybillParser.Parse("7768.DBF");
			Assert.AreEqual(doc.ProviderDocumentId, "Pс060464");
			Assert.AreEqual(doc.DocumentDate.Value.ToShortDateString(), "11.11.2016");
			Assert.AreEqual(doc.Invoice.Amount, 3324.15);
			var line = doc.Lines[0];
			Assert.AreEqual(line.Code, "914875");
			Assert.AreEqual(line.Product, "БЕЛЛА СЕНИ пеленки 30шт, софт 90*60");
			Assert.AreEqual(line.Quantity, 3);
			Assert.AreEqual(line.Nds, 10);
			Assert.AreEqual(line.NdsAmount, 209.09);
			Assert.AreEqual(line.SupplierCost, 766.66);
			Assert.AreEqual(line.SupplierCostWithoutNDS, 696.96);
			Assert.AreEqual(line.Amount, 2299.98);
		}

		[Test]
		public void ParsePustolyakov()
		{
			var doc = WaybillParser.Parse("00041962.DBF");
			Assert.That(doc.Parser, Is.EqualTo("Flagrans19777Parser"));
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
			Assert.AreEqual(line.EAN13, 4690214107426);
		}
	}
}
