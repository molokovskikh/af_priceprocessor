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

                .DocumentInvoice(i => i.InvoiceNumber, "AC_NUM")
                .DocumentInvoice(i => i.InvoiceDate, "AC_DATE")
                .DocumentInvoice(i => i.SellerName, "POST")
                .DocumentInvoice(i => i.SellerAddress, "POST_AD")
                .DocumentInvoice(i => i.SellerINN, "POST_INN")
                .DocumentInvoice(i => i.SellerKPP, "POST_KPP")
                .DocumentInvoice(i => i.ShipperInfo, "GRUZ_POST")
                .DocumentInvoice(i => i.ConsigneeInfo, "GRUZ_GIVE")
                .DocumentInvoice(i => i.PaymentDocumentInfo, "FINDOC")
                .DocumentInvoice(i => i.BuyerName, "BY_NAME")
                .DocumentInvoice(i => i.BuyerAddress, "BY_AD")
                .DocumentInvoice(i => i.BuyerINN, "BY_INN")
                .DocumentInvoice(i => i.BuyerKPP, "BY_KPP")
                .DocumentInvoice(i => i.AmountWithoutNDS0, "SNNDS0")
                .DocumentInvoice(i => i.AmountWithoutNDS10, "SNNDS10")
                .DocumentInvoice(i => i.NDSAmount10, "NDS10")
                .DocumentInvoice(i => i.Amount10, "SNDS10")
                .DocumentInvoice(i => i.AmountWithoutNDS18, "SNNDS18")
                .DocumentInvoice(i => i.NDSAmount18, "NDS18")
                .DocumentInvoice(i => i.Amount18, "SNDS18")
                .DocumentInvoice(i => i.AmountWithoutNDS, "TSNNDS")
                .DocumentInvoice(i => i.NDSAmount, "TNDS")
                .DocumentInvoice(i => i.Amount, "TS")

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
