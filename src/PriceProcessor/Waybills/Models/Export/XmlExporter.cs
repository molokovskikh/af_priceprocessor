using System;
using System.Collections.Generic;
using System.Globalization;
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

		//формат для импорта в ИНПРО-Аптека
		public static void SaveInpro(Document doc, DocumentReceiveLog log, string filename, List<SupplierMap> supplierMaps)
		{
			log.FileName = $"interdoc_{Guid.NewGuid().ToString().Replace("-", "")}.xml";
			log.PreserveFilename = true;
			//не верь решарперу Null можно
			var xmlsettings = new XmlWriterSettings { Encoding = Encoding.GetEncoding(1251) };
			using (var writer = XmlWriter.Create(filename, xmlsettings)) {
				writer.WriteStartElement("DOCUMENTS");
				writer.WriteStartElement("DOCUMENT");
				writer.WriteAttributeString("type", "АПТЕКА_ПРИХОД");
				writer.WriteStartElement("HEADER");
				var supplierName = supplierMaps.FirstOrDefault(x => x.Supplier.Id == doc.Log.Supplier.Id)?.Name
					?? doc.Log.Supplier.FullName;
				writer.WriteAttributeString("firm_name", supplierName);
				writer.WriteAttributeString("client_name", doc.Address.Client.FullName);
				writer.WriteAttributeString("doc_number", doc.ProviderDocumentId);
				writer.WriteAttributeString("factura_number", doc.Invoice?.InvoiceNumber ?? doc.ProviderDocumentId);
				writer.WriteAttributeString("doc_date", doc.DocumentDate?.ToString("dd.MM.yy"));
				writer.WriteAttributeString("pay_date", doc.DocumentDate?.ToString("dd.MM.yy"));
				writer.WriteAttributeString("doc_sum",
					(doc?.Invoice?.Amount ?? doc.Lines.Sum(x => x.Amount))?.ToString(CultureInfo.InvariantCulture));
				writer.WriteEndElement();
				foreach (var line in doc.Lines) {
					writer.WriteStartElement("DETAIL");
					writer.WriteAttributeString("ean13_code", line.EAN13);
					writer.WriteAttributeString("tov_code", line.Code);
					writer.WriteAttributeString("tov_name", line.Product);
					writer.WriteAttributeString("maker_name", line.Producer);
					writer.WriteAttributeString("tov_godn", line.Period);
					writer.WriteAttributeString("tov_seria", line.SerialNumber);
					writer.WriteAttributeString("kolvo", line.Quantity?.ToString(CultureInfo.InvariantCulture));
					writer.WriteAttributeString("maker_price", line.ProducerCost?.ToString(CultureInfo.InvariantCulture));
					writer.WriteAttributeString("firm_price", line.SupplierCostWithoutNDS?.ToString(CultureInfo.InvariantCulture));
					writer.WriteAttributeString("firm_nds", line.NdsAmount?.ToString());
					writer.WriteAttributeString("firm_sum", line.Amount?.ToString(CultureInfo.InvariantCulture));
					writer.WriteAttributeString("firm_nac", line.SupplierPriceMarkup?.ToString(CultureInfo.InvariantCulture));
					writer.WriteAttributeString("gtd_country", line.Country);
					writer.WriteAttributeString("gtd_number", line.BillOfEntryNumber);
					writer.WriteAttributeString("sert_number", line.Certificates);
					writer.WriteAttributeString("sert_godn", line.CertificatesEndDate?.ToString("dd.MM.yy"));
					writer.WriteAttributeString("firm_nds_orig", line.NdsAmount?.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
				}
				writer.WriteEndElement();//DOCUMENT
				writer.WriteEndElement();//DOCUMENTS
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