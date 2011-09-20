using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
    public class ZdravServiceSpecialParser : IDocumentParser
    {
        public static DataTable Load(string file)
        {
            try
            {
                return Dbf.Load(file);
            }
            catch (DbfException)
            {
                return Dbf.Load(file, Encoding.GetEncoding(866), true, false);
            }
        }

        public Document Parse(string file, Document document)
        {          
            var data = Load(file);
            new DbfParser()
                .DocumentHeader(d => d.ProviderDocumentId, "NDOC")
                .DocumentHeader(d => d.DocumentDate, "DATEDOC")

                .DocumentInvoice(i => i.InvoiceNumber, "BILLNUM")
                .DocumentInvoice(i => i.InvoiceDate, "BILLDT")
                .DocumentInvoice(i => i.SellerName, "PROVIDER")
                .DocumentInvoice(i => i.SellerAddress, "PADDR")
                .DocumentInvoice(i => i.SellerINN, "PINNKPP")
                .DocumentInvoice(i => i.SellerKPP, "PINNKPP")
                .DocumentInvoice(i => i.ShipperInfo, "CONSIGNOR")
                .DocumentInvoice(i => i.ConsigneeInfo, "CONSIGNEE")
                .DocumentInvoice(i => i.PaymentDocumentInfo, "NPAYDOC")
                .DocumentInvoice(i => i.BuyerName, "PAYER")
                .DocumentInvoice(i => i.BuyerAddress, "PAYERADDR")
                .DocumentInvoice(i => i.BuyerINN, "PAYERINNKPP")
                .DocumentInvoice(i => i.BuyerKPP, "PAYERINNKPP")
                .DocumentInvoice(i => i.AmountWithoutNDS0, "SUM0")
                .DocumentInvoice(i => i.AmountWithoutNDS10, "SUM10")
                .DocumentInvoice(i => i.NDSAmount10, "NDS10")
                .DocumentInvoice(i => i.Amount10, "SUMNDS10")
                .DocumentInvoice(i => i.AmountWithoutNDS18, "SUM18")
                .DocumentInvoice(i => i.NDSAmount18, "NDS18")
                .DocumentInvoice(i => i.Amount18, "SUMNDS18")
                .DocumentInvoice(i => i.AmountWithoutNDS, "SUMPAYNDS")
                .DocumentInvoice(i => i.NDSAmount, "SUMNDS")
                .DocumentInvoice(i => i.Amount, "SUMPAY")

                .Line(l => l.Code, "CODEPST")
                .Line(l => l.Product, "NAME")
                .Line(l => l.Unit, "UNITS")
                .Line(l => l.Quantity, "QNT")
                .Line(l => l.Producer, "FIRM")
                .Line(l => l.SupplierCost, "PRICE2")
                .Line(l => l.SupplierCostWithoutNDS, "PRICE2N")
                .Line(l => l.SupplierPriceMarkup, "PROCNDB")
                .Line(l => l.RegistryCost, "REGPRC")
                .Line(l => l.ProducerCost, "PRICE1N")
                .Line(l => l.Nds, "NDS")
                .Line(l => l.NdsAmount, "SUMNDS2")
                .Line(l => l.Amount, "SUM1")
                .Line(l => l.SerialNumber, "SER")
                .Line(l => l.Period, "GDATE")
                .Line(l => l.Certificates, "SERTIF")
                .Line(l => l.Country, "CNTR")
                .Line(l => l.BillOfEntryNumber, "NUMGTD")
                .Line(l => l.VitallyImportant, "GNVLS")
                .Line(l => l.EAN13, "EAN13")
                .ToDocument(document, data);

            if (document.Invoice != null)
            {
                if (document.Invoice.SellerINN != null)
                {
                    var innkpp = document.Invoice.SellerINN.Split('/');
                    if (innkpp.Length == 2)
                    {
                        document.Invoice.SellerINN = innkpp[0];
                        document.Invoice.SellerKPP = innkpp[1];
                    }
                }
                if (document.Invoice.BuyerINN != null)
                {
                    var innkpp = document.Invoice.BuyerINN.Split('/');
                    if (innkpp.Length == 2)
                    {
                        document.Invoice.BuyerINN = innkpp[0];
                        document.Invoice.BuyerKPP = innkpp[1];
                    }
                }
            }
            return document;
        }

        public static bool CheckFileFormat(DataTable data)
        {
            return data.Columns.Contains("NDOC")
                   && data.Columns.Contains("DATEDOC")
                   && data.Columns.Contains("PROVIDER")
                   && data.Columns.Contains("PADDR")
                   && data.Columns.Contains("PINNKPP")
                   && data.Columns.Contains("CONSIGNOR")
                   && data.Columns.Contains("CONSIGNEE")
                   && data.Columns.Contains("NPAYDOC")
                   && data.Columns.Contains("PAYER")
                   && data.Columns.Contains("CODEPST")
                   && data.Columns.Contains("EAN13")
                   && data.Columns.Contains("QNT")
                   && data.Columns.Contains("UNITS")
                   && data.Columns.Contains("PRICE2N")
                   && data.Columns.Contains("BILLNUM")
                   && data.Columns.Contains("BILLDT");
        }
    }
}
