using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class ForaFarmVoronezhNewParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NUM_DOC")
				.DocumentHeader(d => d.DocumentDate, "DATE_DOC")
				.Invoice(i => i.InvoiceNumber, "AC_NUM")
				.Invoice(i => i.InvoiceDate, "AC_DATE")
				.Invoice(i => i.SellerName, "POST")
				.Invoice(i => i.SellerAddress, "POST_AD")
				.Invoice(i => i.SellerINN, "POST_INN")
				.Invoice(i => i.SellerKPP, "POST_KPP")
				.Invoice(i => i.ShipperInfo, "GRUZ_POST")
				.Invoice(i => i.RecipientAddress, "GRUZ_GIVE")
				.Invoice(i => i.PaymentDocumentInfo, "FINDOC")
				.Invoice(i => i.BuyerName, "BY_NAME")
				.Invoice(i => i.BuyerAddress, "BY_AD")
				.Invoice(i => i.BuyerINN, "BY_INN")
				.Invoice(i => i.BuyerKPP, "BY_KPP")
				.Invoice(i => i.AmountWithoutNDS0, "SNNDS0")
				.Invoice(i => i.AmountWithoutNDS10, "SNNDS10")
				.Invoice(i => i.NDSAmount10, "NDS10")
				.Invoice(i => i.Amount10, "SNDS10")
				.Invoice(i => i.AmountWithoutNDS18, "SNNDS18")
				.Invoice(i => i.NDSAmount18, "NDS18")
				.Invoice(i => i.Amount18, "SNDS18")
				.Invoice(i => i.AmountWithoutNDS, "TSNNDS")
				.Invoice(i => i.NDSAmount, "TNDS")
				.Invoice(i => i.Amount, "TS")
				.Line(l => l.Product, "NAME_TOVAR")
				.Line(l => l.Unit, "MEASURE")
				.Line(l => l.Quantity, "VOLUME")
				.Line(l => l.Producer, "PROIZ")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.SupplierCostWithoutNDS, "NNDS")
				.Line(l => l.ExciseTax, "AKCIZE")
				.Line(l => l.SupplierPriceMarkup, "PR")
				.Line(l => l.RegistryCost, "GPRICE")
				.Line(l => l.ProducerCostWithoutNDS, "MNNDS")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.NdsAmount, "SUMNDS")
				.Line(l => l.Amount, "SUMMA")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.Period, "GDATE")
				.Line(l => l.Certificates, "DOCUMENT")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.CertificatesDate, "SROKSERT")
				.Line(l => l.VitallyImportant, "ZHNVLS")
				.Line(l => l.EAN13, "EAN13");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("AC_NUM")
				&& data.Columns.Contains("AC_DATE")
				&& data.Columns.Contains("NUM_DOC")
				&& data.Columns.Contains("DATE_DOC")
				&& data.Columns.Contains("POST")
				&& data.Columns.Contains("POST_AD")
				&& data.Columns.Contains("POST_INN")
				&& data.Columns.Contains("POST_KPP")
				&& data.Columns.Contains("NAME_TOVAR")
				&& data.Columns.Contains("PRICE")
				&& data.Columns.Contains("NNDS")
				&& data.Columns.Contains("MPRICE")
				&& data.Columns.Contains("GDATE");
		}
	}
}