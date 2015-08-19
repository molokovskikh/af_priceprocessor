using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Models.Export;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class XmlExportFixture : IntegrationFixture
	{
		[Test]
		public void Export_xml()
		{
			var supplier = TestSupplier.CreateNaked(session);
			var client = TestClient.CreateNaked(session);
			var settings = session.Load<WaybillSettings>(client.Id);
			settings.AssortimentPriceId = supplier.Prices[0].Id;
			settings.WaybillConvertFormat = WaybillFormat.InfoDrugstoreXml;
			var intersection = session.Query<TestAddressIntersection>()
				.First(i => i.Address == client.Addresses[0] && i.Intersection.Price == supplier.Prices[0]);
			intersection.SupplierDeliveryId = "sdf934";
			session.Transaction.Commit();

			var document = new Document(new DocumentReceiveLog(
				session.Load<Supplier>(supplier.Id),
				session.Load<Address>(client.Addresses[0].Id))) {
					DocumentDate = new DateTime(2014, 3, 7)
				};
			var line = document.NewLine();
			line.Code = "21603";
			line.Product = "Алька-прим шип.таб. Х10";
			line.Producer = "Polfa/Polpharma";
			line.Quantity = 13;

			Exporter.Convert(document, document.Log, settings.WaybillConvertFormat, settings);
			var filename = document.Log.GetRemoteFileNameExt();
			var doc = XDocument.Load(filename);
			Assert.AreEqual("0_Тестовый поставщик(0).xml", Path.GetFileName(filename));
			var xml = @"<PACKET NAME=""Электронная накладная"" ID="""" FROM=""Тестовый поставщик"" TYPE=""12"">
  <SUPPLY>
    <INVOICE_DATE>07.03.2014</INVOICE_DATE>
    <DEP_ID>sdf934</DEP_ID>
    <ITEMS>
      <ITEM>
        <CODE>21603</CODE>
        <NAME>Алька-прим шип.таб. Х10</NAME>
        <VENDOR>Polfa/Polpharma</VENDOR>
        <QTTY>13</QTTY>
      </ITEM>
    </ITEMS>
  </SUPPLY>
</PACKET>";
			Assert.AreEqual(xml, doc.ToString());
		}

		[Test]
		public void Export_inpro()
		{
			var doc = new Document(new DocumentReceiveLog(new Supplier { FullName = "ООО «Органика»" }, new Address(new Client {
				FullName = "ИП Бокова А.Б."
			}))) {
				ProviderDocumentId = "О-341720",
				DocumentDate = new DateTime(2015, 07, 14)
			};
			doc.Lines.Add(new DocumentLine {
				Product = "Ибуклин(таб. 400 мг+325 мг №10) Др.Реддис Лабораториес Лтд-Индия",
				Producer = "Др.Реддис Лабораториес Лтд",
				Period = "01.01.2020",
				SerialNumber = "A500178",
				Quantity = 10,
				ProducerCost = 78.84m,
				SupplierCostWithoutNDS = 80.35m,
				NdsAmount = 80.30m,
				Amount = 883.80m,
				SupplierPriceMarkup = 1.915m,
				Country = "Индия",
				BillOfEntryNumber = "10002010/310315/0015516/1",
				Certificates = "РОСС IN.ФМ08.Д57196",
				CertificatesEndDate = new DateTime(2016, 02, 01),
				Code = "607298",
				EAN13 = "8901148232037",
			});
			XmlExporter.SaveInpro(doc, "test.xml", new List<SupplierMap> {
				new SupplierMap {
					Supplier = doc.Log.Supplier,
					Name = "ООО \"Органика\"",
				}
			});
			Assert.That(doc.Log.FileName, Is.StringStarting("interdoc_"));
			var text = File.ReadAllText("test.xml", Encoding.GetEncoding(1251));
			Assert.AreEqual("<?xml version=\"1.0\" encoding=\"windows-1251\"?><DOCUMENTS><DOCUMENT type=\"АПТЕКА_ПРИХОД\">" +
				"<HEADER firm_name=\"ООО &quot;Органика&quot;\" client_name=\"ИП Бокова А.Б.\" doc_number=\"О-341720\" factura_number=\"О-341720\" doc_date=\"14.07.15\" pay_date=\"14.07.15\" doc_sum=\"883.80\" />" +
				"<DETAIL ean13_code=\"8901148232037\" tov_code=\"607298\" tov_name=\"Ибуклин(таб. 400 мг+325 мг №10) Др.Реддис Лабораториес Лтд-Индия\" maker_name=\"Др.Реддис Лабораториес Лтд\" tov_godn=\"01.01.2020\" tov_seria=\"A500178\" kolvo=\"10\" maker_price=\"78.84\" firm_price=\"80.35\" firm_nds=\"80,30\" firm_sum=\"883.80\" firm_nac=\"1.915\" gtd_country=\"Индия\" gtd_number=\"10002010/310315/0015516/1\" sert_number=\"РОСС IN.ФМ08.Д57196\" sert_godn=\"01.02.16\" firm_nds_orig=\"80.30\" />" +
				"</DOCUMENT></DOCUMENTS>", text);
		}
	}
}