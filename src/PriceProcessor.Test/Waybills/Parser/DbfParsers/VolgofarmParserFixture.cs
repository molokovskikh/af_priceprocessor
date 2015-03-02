using System;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	internal class VolgofarmParserFixture
	{
		/// <summary>
		/// Тест для задачи http://redmine.analit.net/issues/32085
		/// </summary>
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("5066_5014C.dbf");
			Assert.IsNotNull(doc, "Накладная не была обработана");
			Assert.IsTrue(doc.Parser == "VolgofarmParser", "Обработал парсер " + doc.Parser);
			Assert.IsTrue(doc.ProviderDocumentId == "5014C");
			Assert.IsTrue(doc.DocumentDate == Convert.ToDateTime("17.02.2015"));

			var invoice = doc.Invoice;
			Assert.IsTrue(invoice.Amount == 842.2m);
			Assert.IsTrue(invoice.NDSAmount10 == 76.56m);
			Assert.IsTrue(invoice.NDSAmount18 == 0m);
			Assert.IsTrue(invoice.AmountWithoutNDS10 == 765.64m);
			Assert.IsTrue(invoice.AmountWithoutNDS18 == 0m);
			Assert.IsTrue(invoice.AmountWithoutNDS0 == 0m);
			Assert.IsTrue(invoice.RecipientAddress == "04501037");

			var line0 = doc.Lines[0];
			Assert.IsTrue(line0.Code == "78128");
			Assert.IsTrue(line0.Product == "ИНДАП 2.5МГ N30 КАПС");
			Assert.IsTrue(line0.Producer == "ПРО.МЕД.ЦС");
			Assert.IsTrue(line0.Country == "ЧЕХИЯ");
			Assert.IsTrue(line0.SupplierCostWithoutNDS == 76.5636m);
			Assert.IsTrue(line0.SupplierCost == 84.22m);
			Assert.IsTrue(line0.Quantity == 10u);
			Assert.IsTrue(line0.Nds == 10u);
			Assert.IsTrue(line0.Period == "01.08.2017");
			Assert.IsTrue(line0.RegistryCost == 68.36m);
			Assert.IsTrue(line0.VitallyImportant == true);
			Assert.IsTrue(line0.SerialNumber == "0570814");
			Assert.IsTrue(line0.EAN13 == "8595026461338");
			Assert.IsTrue(line0.Certificates == "РОСС CZ.ФМ08.Д41976");
			Assert.IsTrue(line0.CertificateAuthority == "ООО ОЦКК  г. Москва");
			Assert.IsTrue(line0.CertificatesDate == "24.12.2014");
			Assert.IsTrue(line0.OrderId == 0u);
			Assert.IsTrue(line0.BillOfEntryNumber == "10130032/241214/0009781/1");
			Assert.IsTrue(line0.ProducerCostWithoutNDS == 68.36m);
			Assert.IsTrue(line0.Amount == 751.96m);
		}
	}
}
