using System;
using System.Linq;
using System.Text;
using System.Xml;
using Common.Tools;
using NHibernate;

namespace Inforoom.PriceProcessor.Waybills.Models.Export
{
	public class XmlExporter
	{
		public static void SaveInfoDrugstore(ISession session, WaybillSettings settings, Document document, string filename)
		{
			var addressId = session.CreateSQLQuery(@"
select ai.SupplierDeliveryId
from Customers.Intersection i
	join Customers.AddressIntersection ai on ai.IntersectionId = i.Id
where ai.AddressId = :addressId
	and ai.SupplierDeliveryId is not null
	and i.PriceId = :priceId
group by ai.SupplierDeliveryId")
				.SetParameter("addressId", document.Address.Id)
				.SetParameter("priceId", settings.AssortimentPriceId)
				.List<string>()
				.FirstOrDefault();

			var xmlsettings = new XmlWriterSettings { Encoding = Encoding.GetEncoding(1251) };
			using (var writer = XmlWriter.Create(filename, xmlsettings)) {
				writer.WriteStartDocument(true);
				writer.WriteStartElement("PACKET");
				writer.WriteAttributeString("NAME", "Электронная накладная");
				writer.WriteAttributeString("ID", document.ProviderDocumentId);
				writer.WriteAttributeString("FROM", document.Log.Supplier.Name);
				writer.WriteAttributeString("TYPE", "12");

				writer.WriteStartElement("SUPPLY");
				writer.Element("INVOICE_NUM", document.ProviderDocumentId);
				writer.Element("INVOICE_DATE", document.DocumentDate.Value.ToString("dd.MM.yyyy"));
				writer.Element("DEP_ID", addressId);
				writer.Element("ORDER_ID", document.OrderId);

				writer.WriteStartElement("ITEMS");
				foreach (var line in document.Lines) {
					writer.WriteStartElement("ITEM");
					writer.Element("CODE", line.ExportCode);
					writer.Element("NAME", line.ExportProduct);
					writer.Element("VENDOR", line.ExportProducer);
					writer.Element("QTTY", line.Quantity);
					writer.Element("SPRICE", line.SupplierCostWithoutNDS);
					writer.Element("VPRICE", line.ProducerCostWithoutNDS);
					writer.Element("NDS", line.Nds);
					writer.Element("SNDSSUM", line.NdsAmount);
					writer.Element("SERIA", line.SerialNumber);
					writer.Element("VALID_DATE", line.Period);
					writer.Element("GTD", line.BillOfEntryNumber);
					writer.Element("SERT_NUM", line.Certificates);
					writer.Element("VENDORBARCODE", line.EAN13.Slice(12));
					writer.Element("REG_PRICE", line.RegistryCost);
					writer.Element("ISGV", line.VitallyImportant);
					writer.WriteEndElement();
				}
				writer.WriteEndElement(); //ITEMS

				writer.WriteEndElement(); //SUPPLY

				writer.WriteEndElement(); //PACKET
			}
		}
	}

	public static class XmlWriterExtentions
	{
		public static void Element(this XmlWriter writer, string name, object value)
		{
			value = NullableHelper.GetNullableValue(value);
			if (value == null
				|| value.Equals(String.Empty))
				return;

			if (value is bool) {
				value = (bool)value ? 1 : 0;
			}

			writer.WriteElementString(name, value.ToString());
		}
	}
}