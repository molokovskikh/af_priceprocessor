using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class ForaFarmParserFixture
	{
		[Test]
		public void Parse()
		{
			var waybill = WaybillParser.Parse("ФФ_СПБ_Nak06145300.dbf");
			Assert.AreEqual("06145300", waybill.ProviderDocumentId);
			Assert.AreEqual("27.06.2014", waybill.DocumentDate.Value.ToShortDateString());
			var line = waybill.Lines[0];
			Assert.AreEqual("Неогален Ревматин гель-бальзам/тела 100мл", line.Product);
			Assert.AreEqual("РОССИЯ", line.Country);
			Assert.AreEqual("7640123861695", line.EAN13);
			Assert.AreEqual("ТС RU Д-RU.АЮ18.В.02072", line.Certificates);
			Assert.AreEqual("02.12.2018", line.CertificatesDate);
			Assert.AreEqual("ТС ЕВРАЗЭС ОС ООО \"Сергиево-Посадский ЦСМ\"", line.CertificateAuthority);
			Assert.AreEqual("01.03.2016", line.Period);
			Assert.AreEqual(40.91, line.SupplierCost);
			Assert.AreEqual(2, line.Quantity);
			Assert.AreEqual(18, line.Nds);
		}
	}
}