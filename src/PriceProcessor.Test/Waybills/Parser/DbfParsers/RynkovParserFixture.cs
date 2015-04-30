using System;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	internal class RynkovParserFixture
	{
		/// <summary>
		/// Тест для задачи http://redmine.analit.net/issues/33581
		/// </summary>
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("Waybill_IB_Rynkov.dbf");
			Assert.IsNotNull(doc, "Накладная не была обработана");
			Assert.IsTrue(doc.Parser == "RynkovParser", "Обработал парсер " + doc.Parser);
			Assert.IsTrue(doc.ProviderDocumentId == "002665");
			Assert.IsTrue(doc.DocumentDate == Convert.ToDateTime("16.04.2015"));

			var line0 = doc.Lines[0];
			Assert.IsTrue(line0.Code == "0000001583");
			Assert.IsTrue(line0.Product == "Тоник Медовый 100 мл");
			Assert.IsTrue(line0.EAN13 == null);
			Assert.IsTrue(line0.SupplierCostWithoutNDS == 16.5m);
			Assert.IsTrue(line0.SupplierCost == 16.5m);
			Assert.IsTrue(line0.Quantity == 50u);
			Assert.IsTrue(line0.SerialNumber == "РОСС RU.АЯ70.Д04382");
			Assert.IsTrue(line0.RegistryDate == null);
			Assert.IsTrue(line0.DateOfManufacture == null);
			Assert.IsTrue(line0.Country == "Казахстан");
			Assert.IsTrue(line0.Producer == "ПК СПК \"Алмалыбак\"");
			Assert.IsTrue(line0.Nds == 0u);
			Assert.IsTrue(line0.VitallyImportant == false);
			Assert.IsTrue(line0.BillOfEntryNumber == null);
			Assert.IsTrue(line0.Certificates == "РОСС RU.АЯ70.Д04382");
			Assert.IsTrue(line0.CertificatesDate == "22.09.2013");
			Assert.IsTrue(line0.Period == "22.09.2016");
			Assert.IsTrue(line0.CertificateAuthority == 
				"РОСС RU.0001.10AЯ70 ПИЩЕВОЙ, СЕЛЬСКОХОЗЯЙСТВЕННОЙ, ПАРФЮМЕРНО-КОСМЕТИЧЕСКОЙ ПРОД");
			Assert.IsTrue(line0.Amount == 825m);
			Assert.IsTrue(line0.NdsAmount == 0m);

			var invoice = doc.Invoice;
			Assert.IsTrue(invoice.Amount == 3245m);
			Assert.IsTrue(invoice.NDSAmount == 0m);
		}
	}
}
