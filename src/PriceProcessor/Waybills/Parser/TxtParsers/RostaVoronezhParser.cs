using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
    public class RostaVoronezhParser : BaseIndexingParser
    {
        protected virtual void SetIndexes()
        {
            InvoiceNumberIndex = 0;
            InvoiceDateIndex = 1;
            ProviderDocumentIdIndex = 2;
            DocumentDateIndex = 3;
            SellerNameIndex = 4;
            SellerAddressIndex = 5;
            SellerINNIndex = 6;
            SellerKPPIndex = 6;
            ShipperInfoIndex = 7;
            ConsigneeInfoIndex = 8;            
            BuyerNameIndex = 9;
            BuyerAddressIndex = 10;
            BuyerINNIndex = 11;
            BuyerKPPIndex = 11;
            AmountWithoutNDS0Index = 12;
            AmountWithoutNDS10Index = 13;
            NDSAmount10Index = 14;
            Amount10Index = 15;
            AmountWithoutNDS18Index = 16;
            NDSAmount18Index = 17;
            Amount18Index = 18;
            AmountWithoutNDSIndex = 19;
            InvoiceNDSAmountIndex = 20;
            InvoiceAmountIndex = 21;

            ProductIndex = 1;
            UnitIndex = 2;
            QuantityIndex = 3;
            ProducerIndex = 4;
            SupplierCostIndex = 5;
            SupplierCostWithoutNdsIndex = 6;            
            SupplierPriceMarkupIndex = 8;
            RegistryCostIndex = 9;
            ProducerCostIndex = 11;
            NdsIndex = 12;
            NdsAmountIndex = 13;
            AmountIndex = 14;
            SerialNumberIndex = 15;
            PeriodIndex = 16;
            CertificatesIndex = 17;
            CountryIndex = 18;
            BillOfEntryNumberIndex = 19;
            CertificatesDateIndex = 20;
            VitallyImportantIndex = 21;
            EAN13Index = 22;
        }

        public override Document Parse(string file, Document document)
        {
            SetIndexes();
            using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
            {
                var headers = reader.ReadLine().Split(';');

                document.SetInvoice().InvoiceNumber = GetString(headers[InvoiceNumberIndex]);
                document.SetInvoice().InvoiceDate = GetDateTime(headers[InvoiceDateIndex]);
                document.ProviderDocumentId = GetString(headers[ProviderDocumentIdIndex]);
                document.DocumentDate = GetDateTime(headers[DocumentDateIndex]);
                document.SetInvoice().SellerName = GetString(headers[SellerNameIndex]);
                document.SetInvoice().SellerAddress = GetString(headers[SellerAddressIndex]);
                if (headers[SellerINNIndex].Contains("/"))
                {
                    document.SetInvoice().SellerINN = GetString(headers[SellerINNIndex].Split('/')[0]);
                    document.SetInvoice().SellerKPP = GetString(headers[SellerINNIndex].Split('/')[1]);
                }
                document.SetInvoice().ShipperInfo = GetString(headers[ShipperInfoIndex]);
                document.SetInvoice().ConsigneeInfo = GetString(headers[ConsigneeInfoIndex]);
                document.SetInvoice().BuyerName = GetString(headers[BuyerNameIndex]);
                document.SetInvoice().BuyerAddress = GetString(headers[BuyerAddressIndex]);
                if (headers[BuyerINNIndex].Contains("/"))
                {
                    document.SetInvoice().BuyerINN = GetString(headers[BuyerINNIndex].Split('/')[0]);
                    document.SetInvoice().BuyerKPP = GetString(headers[BuyerINNIndex].Split('/')[1]);
                }
                document.SetInvoice().AmountWithoutNDS0 = GetDecimal(headers[AmountWithoutNDS0Index]);
                document.SetInvoice().AmountWithoutNDS10 = GetDecimal(headers[AmountWithoutNDS10Index]);
                document.SetInvoice().NDSAmount10 = GetDecimal(headers[NDSAmount10Index]);
                document.SetInvoice().Amount10 = GetDecimal(headers[Amount10Index]);
                document.SetInvoice().AmountWithoutNDS18 = GetDecimal(headers[AmountWithoutNDS18Index]);
                document.SetInvoice().NDSAmount18 = GetDecimal(headers[NDSAmount18Index]);
                document.SetInvoice().Amount18 = GetDecimal(headers[Amount18Index]);
                document.SetInvoice().AmountWithoutNDS = GetDecimal(headers[AmountWithoutNDSIndex]);
                document.SetInvoice().NDSAmount = GetDecimal(headers[InvoiceNDSAmountIndex]);
                document.SetInvoice().Amount = GetDecimal(headers[InvoiceAmountIndex]);

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split(';');
                    var docLine = document.NewLine();
                    docLine.Product = GetString(parts[ProductIndex]);
                    docLine.Unit = GetString(parts[UnitIndex]);
                    docLine.Quantity = (uint?)GetInteger(parts[QuantityIndex]);
                    docLine.Producer = GetString(parts[ProducerIndex]);
                    docLine.SupplierCost = GetDecimal(parts[SupplierCostIndex]);
                    docLine.SupplierCostWithoutNDS = GetDecimal(parts[SupplierCostWithoutNdsIndex]);
                    docLine.SupplierPriceMarkup = GetDecimal(parts[SupplierPriceMarkupIndex]);
                    docLine.RegistryCost = GetDecimal(parts[RegistryCostIndex]);
                    docLine.ProducerCost = GetDecimal(parts[ProducerCostIndex]);
                    docLine.Nds = (uint?)GetInteger(parts[NdsIndex]);
                    docLine.NdsAmount = GetDecimal(parts[NdsAmountIndex]);
                    docLine.Amount = GetDecimal(parts[AmountIndex]);
                    docLine.SerialNumber = GetString(parts[SerialNumberIndex]);
                    docLine.Period = GetString(parts[PeriodIndex]);
                    docLine.Certificates = GetString(parts[CertificatesIndex]);
                    docLine.Country = GetString(parts[CountryIndex]);
                    docLine.BillOfEntryNumber = GetString(parts[BillOfEntryNumberIndex]);
                    docLine.CertificatesDate = GetString(parts[CertificatesDateIndex]);
                    docLine.VitallyImportant = GetBool(parts[VitallyImportantIndex]);
                    docLine.EAN13 = GetString(parts[EAN13Index]);
                }
            }
            return document;
        }

        public static bool CheckFileFormat(string file)
        {
            using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
            {
                var headers = reader.ReadLine().Split(';');
                if (headers.Length < 22)
                    return false;

                DateTime dt;
                if (!DateTime.TryParse(headers[1], out dt) ||
                    !DateTime.TryParse(headers[3], out dt))
                    return false;
                if(!headers[6].Contains("/") ||
                   !headers[11].Contains("/"))
                    return false;
                string line;
                while ((line = reader.ReadLine()) != null)
                    if (line.Split(';').Length < 23) return false;
                return true;
            }
        }
    }
}
