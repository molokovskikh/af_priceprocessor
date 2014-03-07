using System.IO;
using System.Linq;
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
			var supplier = TestSupplier.CreateNaked();
			var client = TestClient.CreateNaked();
			var settings = session.Load<WaybillSettings>(client.Id);
			settings.AssortimentPriceId = supplier.Prices[0].Id;
			var intersection = session.Query<TestAddressIntersection>()
				.First(i => i.Address == client.Addresses[0] && i.Intersection.Price == supplier.Prices[0]);
			intersection.SupplierDeliveryId = "sdf934";
			session.Flush();

			var document = new Document(new DocumentReceiveLog(
				session.Load<Supplier>(supplier.Id),
				session.Load<Address>(client.Addresses[0].Id)));
			var line = document.NewLine();
			line.Code = "21603";
			line.Product = "Алька-прим шип.таб. Х10";
			line.Producer = "Polfa/Polpharma";
			line.Quantity = 13;

			var filename = Path.GetRandomFileName();
			XmlExporter.SaveInfoDrugstore(session, settings, document, filename);
			var doc = XDocument.Load(filename);
			var xml = @"<PACKET NAME=""Электронная накладная"" ID="""" FROM=""Тестовый поставщик"" TYPE=""12"">
  <SUPPLY>
    <INVOICE_DATE>07.03.2014</INVOICE_DATE>
    <DEP_ID>sdf934</DEP_ID>
    <ITEMS>
      <ITEM>
        <CODE>21603</CODE>
        <NAME>Polfa/Polpharma</NAME>
        <VENDOR>Polfa/Polpharma</VENDOR>
        <QTTY>13</QTTY>
      </ITEM>
    </ITEMS>
  </SUPPLY>
</PACKET>";
			Assert.AreEqual(xml, doc.ToString());
		}
	}
}