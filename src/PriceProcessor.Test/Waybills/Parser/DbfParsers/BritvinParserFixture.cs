using System;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	internal class BritvinParserFixture
	{
		/// <summary>
		/// Тест для задачи http://redmine.analit.net/issues/34029
		/// </summary>
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("Waybill_AlefFarm.dbf");
			Assert.IsNotNull(doc, "Накладная не была обработана");
			Assert.IsTrue(doc.Parser == "BritvinParser", "Обработал парсер " + doc.Parser);
			Assert.IsTrue(doc.ProviderDocumentId == "3023");
			Assert.IsTrue(doc.DocumentDate == Convert.ToDateTime("05.05.2015"));

			var line0 = doc.Lines[0];
			Assert.IsTrue(line0.Code == "80006");
			Assert.IsTrue(line0.Product == "COLGATE з/щ ЗигЗаг (сред)");
			Assert.IsTrue(line0.Quantity == 5u);
			Assert.IsTrue(line0.SupplierCostWithoutNDS == 31.542m);
			Assert.IsTrue(line0.NdsAmount == 28.39m);
			Assert.IsTrue(line0.Certificates == "РОСС СN. ПК04. В00342  №6");
			Assert.IsTrue(line0.CertificateAuthority == "ОС АНО \"ЦПС \"Профидент\"");
			Assert.IsTrue(line0.CertificatesDate == "27.09.2016");
			Assert.IsTrue(line0.Period == "01.01.2016");
			Assert.IsTrue(line0.Country == "156 Китай");
			Assert.IsTrue(line0.Producer == "Антикризис  КОЛГЕЙТ-ПАЛМОЛИВ");
			Assert.IsTrue(line0.SupplierCost == 37m);
			Assert.IsTrue(line0.Amount == 186m);
			Assert.IsTrue(line0.Nds == 18u);
			Assert.IsTrue(line0.EAN13 == "7610196003544");

			var invoice = doc.Invoice;
			Assert.IsTrue(invoice.Amount == 1159m);
		}
	}
}
