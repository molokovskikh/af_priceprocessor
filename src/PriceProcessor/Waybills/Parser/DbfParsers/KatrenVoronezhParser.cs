using System;
using System.Data;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class KatrenVoronezhParser : BaseDbfParser
	{
		 public override DbfParser GetParser()
        {
            return new DbfParser()
                .DocumentHeader(d => d.ProviderDocumentId, "QueryNum")
                .DocumentHeader(d => d.DocumentDate, "QueryDate")

                .DocumentInvoice(i => i.InvoiceNumber, "DocNum")
                .DocumentInvoice(i => i.InvoiceDate, "DocDate")
                .DocumentInvoice(i => i.SellerName, "SellerName")
                .DocumentInvoice(i => i.SellerAddress, "SAddress")
                .DocumentInvoice(i => i.SellerINN, "SINN")
                .DocumentInvoice(i => i.SellerKPP, "SINN")
                .DocumentInvoice(i => i.ShipperInfo, "GAddress")
                .DocumentInvoice(i => i.RecipientAddress, "CAddress")
                .DocumentInvoice(i => i.PaymentDocumentInfo, "CAccount")
                .DocumentInvoice(i => i.BuyerName, "Contractor")
                .DocumentInvoice(i => i.BuyerAddress, "StAddress")
                .DocumentInvoice(i => i.BuyerINN, "CINN")
                .DocumentInvoice(i => i.BuyerKPP, "CINN")
                .DocumentInvoice(i => i.AmountWithoutNDS0, "STWONDS0")
                .DocumentInvoice(i => i.AmountWithoutNDS10, "STWONDS10")
                .DocumentInvoice(i => i.NDSAmount10, "SUMNDS10")
                .DocumentInvoice(i => i.Amount10, "SUMTOTAL10")
                .DocumentInvoice(i => i.AmountWithoutNDS18, "STWONDS18")
                .DocumentInvoice(i => i.NDSAmount18, "SUMNDS18")
                .DocumentInvoice(i => i.Amount18, "SUMTOTAL18")
                .DocumentInvoice(i => i.AmountWithoutNDS, "STOTWONDS")
                .DocumentInvoice(i => i.NDSAmount, "SUMNDS")
                .DocumentInvoice(i => i.Amount, "SUMTOTAL")

                .Line(l => l.Product, "GOODE")
                .Line(l => l.Unit, "IZM")
                .Line(l => l.Quantity, "QUANT")
                .Line(l => l.Producer, "PRODUSER")
                .Line(l => l.SupplierCost, "PRICE")
                .Line(l => l.SupplierCostWithoutNDS, "PRICE2")
                .Line(l => l.ExciseTax, "AKZ")
                .Line(l => l.SupplierPriceMarkup, "MARGIN")
                .Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.ProducerCost, "PPRICENDS")
				.Line(l => l.ProducerCostWithoutNDS, "PPRICEWT")
                .Line(l => l.Nds, "NDS")
                .Line(l => l.NdsAmount, "NDSSUM")
                .Line(l => l.Amount, "SPRICENDS")
                .Line(l => l.SerialNumber, "SERIAL")
                .Line(l => l.Period, "DATEB")
                .Line(l => l.Certificates, "SERT")
                .Line(l => l.Country, "COUNTRY")
                .Line(l => l.BillOfEntryNumber, "GTD")
                .Line(l => l.CertificatesDate, "D_SERTIF")
                .Line(l => l.VitallyImportant, "GV")
                .Line(l => l.EAN13, "EAN13")
				.Line(l => l.OrderId, "NUMZAK");
        }

        public override void PostParsing(Document doc)
        {
            if(doc.Invoice != null)
            {
                var innkpp = doc.Invoice.SellerINN.Split('/');
                if (innkpp.Length == 2)
                {
                    doc.Invoice.SellerINN = String.IsNullOrEmpty(innkpp[0]) ? null : innkpp[0];
                    doc.Invoice.SellerKPP = String.IsNullOrEmpty(innkpp[1]) ? null : innkpp[1];
                }
                innkpp = doc.Invoice.BuyerINN.Split('/');
                if(innkpp.Length == 2)
                {
                    doc.Invoice.BuyerINN = String.IsNullOrEmpty(innkpp[0]) ? null : innkpp[0];
                    doc.Invoice.BuyerKPP = String.IsNullOrEmpty(innkpp[1]) ? null : innkpp[1];
                }
            }
        }

        public static bool CheckFileFormat(DataTable data)
        {
            return data.Columns.Contains("DocNum")
                   && data.Columns.Contains("DocDate")
                   && data.Columns.Contains("QueryNum")
                   && data.Columns.Contains("QueryDate")
                   && data.Columns.Contains("SellerName")
                   && data.Columns.Contains("SAddress")
                   && data.Columns.Contains("SINN")
                   && data.Columns.Contains("GAddress")
                   && data.Columns.Contains("GOODE");
        }
	}
}
