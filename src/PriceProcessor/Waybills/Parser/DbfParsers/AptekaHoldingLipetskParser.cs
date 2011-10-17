using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
    public class AptekaHoldingLipetskParser : BaseDbfParser
    {
        public override DbfParser GetParser()
        {
            return new DbfParser()
                .DocumentHeader(d => d.ProviderDocumentId, "H_NAKLNOM")
                .DocumentHeader(d => d.DocumentDate, "H_NAKLDATE")

                .DocumentInvoice(i => i.InvoiceNumber, "H_SFNOM")
                .DocumentInvoice(i => i.InvoiceDate, "H_SFDATE")
                .DocumentInvoice(i => i.SellerName, "H_SELNAME")
                .DocumentInvoice(i => i.SellerAddress, "H_SELADR")
                .DocumentInvoice(i => i.SellerINN, "H_SELINN")
                .DocumentInvoice(i => i.SellerKPP, "H_SELINN")
                .DocumentInvoice(i => i.ShipperInfo, "H_SELOTPR")
                .DocumentInvoice(i => i.ConsigneeInfo, "H_GP")
                .DocumentInvoice(i => i.PaymentDocumentInfo, "H_DOC")
                .DocumentInvoice(i => i.BuyerName, "H_BUYNAME")
                .DocumentInvoice(i => i.BuyerAddress, "H_BUYADR")
                .DocumentInvoice(i => i.BuyerINN, "H_BUYINN")
                .DocumentInvoice(i => i.BuyerKPP, "H_BUYINN")
                .DocumentInvoice(i => i.AmountWithoutNDS0, "H_SUM0")
                .DocumentInvoice(i => i.AmountWithoutNDS10, "H_SUM10")
                .DocumentInvoice(i => i.NDSAmount10, "H_NDSSUM10")
                .DocumentInvoice(i => i.Amount10, "H_SUM10S")
                .DocumentInvoice(i => i.AmountWithoutNDS18, "H_SUM18")
                .DocumentInvoice(i => i.NDSAmount18, "H_NDSSUM18")
                .DocumentInvoice(i => i.Amount18, "H_SUM18S")
                .DocumentInvoice(i => i.AmountWithoutNDS, "H_SUM")
                .DocumentInvoice(i => i.NDSAmount, "H_NDSSUM")
                .DocumentInvoice(i => i.Amount, "H_SUMS")

                .Line(l => l.Product, "B_GOOD")
                .Line(l => l.Unit, "B_ED")
                .Line(l => l.Quantity, "B_KOL")
                .Line(l => l.Producer, "B_PROIZV")
                .Line(l => l.SupplierCost, "B_PRICES")
                .Line(l => l.SupplierCostWithoutNDS, "B_PRICE")
                .Line(l => l.ExciseTax, "B_AKC")
                .Line(l => l.SupplierPriceMarkup, "B_NADB")
                .Line(l => l.RegistryCost, "B_REGC")
				.Line(l => l.ProducerCostWithoutNDS, "B_MAKE")
                .Line(l => l.Nds, "B_NDS")
                .Line(l => l.NdsAmount, "B_NDSSUM")
                .Line(l => l.Amount, "B_SUMS")
                .Line(l => l.SerialNumber, "B_SERIA")
                .Line(l => l.Period, "B_SROKG")
                .Line(l => l.Certificates, "B_SERTN")
                .Line(l => l.Country, "B_STRANA")
                .Line(l => l.BillOfEntryNumber, "B_GTD")
                .Line(l => l.CertificatesDate, "B_SERTD")
                .Line(l => l.VitallyImportant, "B_ISLIFE")
                .Line(l => l.EAN13, "B_EAN13");
        }

        public override void PostParsing(Document doc)
        {
            if(doc.Invoice != null)
            {
                var innkpp = doc.Invoice.SellerINN.Split('/');
                if (innkpp.Length == 2)
                {
                    doc.Invoice.SellerINN = innkpp[0];
                    doc.Invoice.SellerKPP = innkpp[1];
                }
                innkpp = doc.Invoice.BuyerINN.Split('/');
                if(innkpp.Length == 2)
                {
                    doc.Invoice.BuyerINN = innkpp[0];
                    doc.Invoice.BuyerKPP = innkpp[1];
                }
            }
        }

        public static bool CheckFileFormat(DataTable data)
        {
            return data.Columns.Contains("H_NAKLNOM")
                   && data.Columns.Contains("H_NAKLDATE")
                   && data.Columns.Contains("H_SFNOM")
                   && data.Columns.Contains("H_SFDATE")
                   && data.Columns.Contains("H_SELNAME")
                   && data.Columns.Contains("H_SELADR")
                   && data.Columns.Contains("H_SELINN")
                   && data.Columns.Contains("H_SELOTPR")
                   && data.Columns.Contains("B_GOOD");
        }
    }
}
