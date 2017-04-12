using System;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class OriolaParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("39554_1678689_130819.dbf");

			Assert.AreEqual("1678689", doc.ProviderDocumentId);
			Assert.AreEqual(new DateTime(2013, 08, 19), doc.DocumentDate);
			Assert.AreEqual(6, doc.Lines.Count);
			var line = doc.Lines[0];
			Assert.AreEqual("30890", line.Code);
			Assert.AreEqual(4606556000202, line.EAN13);
			Assert.AreEqual("Де-Нол таб. п/о 120мг №112", line.Product);
			Assert.AreEqual(4, line.Quantity);
			Assert.AreEqual(10, line.Nds);
			Assert.AreEqual(299.83, line.NdsAmount);
			Assert.AreEqual(3298.15, line.Amount);
			Assert.AreEqual("Астеллас Фарма Юроп/Ортат", line.Producer);
			Assert.AreEqual("3330", line.CodeCr);
			Assert.AreEqual(804.28, line.ProducerCost);
			Assert.AreEqual(731.16, line.ProducerCostWithoutNDS);
			Assert.AreEqual("Россия", line.Country);
			Assert.AreEqual("55052013", line.SerialNumber);
			Assert.IsTrue(line.VitallyImportant.Value);
			Assert.AreEqual("РОСС.RU.ФМ09.Д56798", line.Certificates);
			Assert.AreEqual("30.06.2013", line.CertificatesDate);
			Assert.AreEqual("ООО \"Институт фармацевтической биотехнологии\"", line.CertificateAuthority);
			Assert.AreEqual(731.16, line.RegistryCost);
			Assert.AreEqual(new DateTime(2010, 11, 11), line.RegistryDate);
			Assert.AreEqual("01.01.2017", line.Period);
		}
	}
}