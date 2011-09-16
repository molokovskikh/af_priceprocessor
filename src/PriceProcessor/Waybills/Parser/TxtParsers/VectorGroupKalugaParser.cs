using System;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser.Helpers;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
    public class VectorGroupKalugaParser : BaseIndexingParser
    {
        protected override void SetIndexes()
        {
            CodeIndex = -1;
            ProductIndex = 1;
            UnitIndex = 2;
            QuantityIndex = 3;
            ProducerIndex = 4;
            SupplierCostWithoutNdsIndex = 5;
            SupplierCostIndex = 6;
            ExciseTaxIndex = 8;
            SupplierPriceMarkupIndex = 9;
            RegistryCostIndex = 10;
            ProducerCostIndex = 12;
            NdsIndex = 13;
            NdsAmountIndex = 14;
            AmountIndex = 15;
            SerialNumberIndex = 16;
            PeriodIndex = 17;
            CertificatesIndex = 18;
            CountryIndex = 19;
            BillOfEntryNumberIndex = 20;
            CertificatesDateIndex = 21;
            VitallyImportantIndex = 22;
            EAN13Index = 23;
        }

        public override Document Parse(string file, Document document)
        {
            SetIndexes();
            using (var parser = new HeaderBodyParser(file, CommentMark))
            {
                var lineCounter = 0;
                const int headerLinesCount = 23;
                foreach (var headline in parser.Header())
                {                    
                    var parts = headline.Split(';');
                    var key = parts[0].ToLower().Trim();                    
                    if (key.ToLower() == "номер накладной")
                        document.ProviderDocumentId = GetString(parts[1]);
                    if (key.ToLower() == "дата накладной")
                        if (!String.IsNullOrEmpty(parts[1]))
                            document.DocumentDate = Convert.ToDateTime(parts[1]);
                    if (key.ToLower() == "номер с/ф")
                        document.SetInvoice().InvoiceNumber = GetString(parts[1]);
                    if (key.ToLower() == "дата с/ф")
                        if (!String.IsNullOrEmpty(parts[1]))
                            document.SetInvoice().InvoiceDate = Convert.ToDateTime(parts[1]);
                    if (key.ToLower() == "продавец")
                        document.SetInvoice().SellerName = GetString(parts[1]);
                    if (key.ToLower() == "адрес продавца")
                        document.SetInvoice().SellerAddress = GetString(parts[1]);
                    if (key.ToLower() == "инн/кпп продавца")
                    {
                        if (parts[1].Contains("/"))
                        {
                            document.SetInvoice().SellerINN = GetString(parts[1].Split('/')[0]);
                            document.SetInvoice().SellerKPP = GetString(parts[1].Split('/')[1]);
                        }
                    }
                    if (key == "грузоотправитель и его адрес")
                        document.SetInvoice().ShipperInfo = GetString(parts[1]);
                    if (key == "грузополучатель и его адрес")
                        document.SetInvoice().ConsigneeInfo = GetString(parts[1]);
                    if (key == "к платежно-расчетному документу n")
                        document.SetInvoice().PaymentDocumentInfo = GetString(parts[1]);
                    if (key == "покупатель")
                        document.SetInvoice().BuyerName = GetString(parts[1]);
                    if (key == "адрес покупателя")
                        document.SetInvoice().BuyerAddress = GetString(parts[1]);
                    if (key == "инн/кпп покупателя")
                    {
                        if (parts[1].Contains("/"))
                        {
                            document.SetInvoice().BuyerINN = GetString(parts[1].Split('/')[0]);
                            document.SetInvoice().BuyerKPP = GetString(parts[1].Split('/')[1]);
                        }
                    }
                    if (key == "стоимость без ндс 0%")
                        document.SetInvoice().AmountWithoutNDS0 = GetDecimal(parts[1]);
                    if (key == "стоимость без ндс 10%")
                        document.SetInvoice().AmountWithoutNDS10 = GetDecimal(parts[1]);
                    if (key == "сумма ндс 10%")
                        document.SetInvoice().NDSAmount10 = GetDecimal(parts[1]);
                    if (key == "стоимость с ндс 10%")
                        document.SetInvoice().Amount10 = GetDecimal(parts[1]);
                    if (key == "стоимость без ндс 18%")
                        document.SetInvoice().AmountWithoutNDS18 = GetDecimal(parts[1]);
                    if (key == "сумма ндс 18%")
                        document.SetInvoice().NDSAmount18 = GetDecimal(parts[1]);
                    if (key == "стоимость с ндс 18%")
                        document.SetInvoice().Amount18 = GetDecimal(parts[1]);
                    if (key == "общая стоимость без ндс")
                        document.SetInvoice().AmountWithoutNDS = GetDecimal(parts[1]);
                    if (key == "общая сумма ндс")
                        document.SetInvoice().NDSAmount = GetDecimal(parts[1]);
                    if (key == "общая стоимость с ндс")
                        document.SetInvoice().Amount = GetDecimal(parts[1]);
                    if (++lineCounter == headerLinesCount) break;
                }
                foreach (var body in parser.Body())
                {
                    var line = body.Split(';');
                    int ival;
                    if (!Int32.TryParse(line[0], out ival)) continue;
                    ReadBody(document, body);
                }
            }
            return document;
        }

        public static bool CheckFileFormat(string file)
        {
            bool isHeader = false;
            bool isBody = false;            
            int counter = 0;
            using (var parser = new HeaderBodyParser(file, null))
            {
                var content = parser.Lines();
                
                foreach(var line in content)
                {
                    if (line.ToLower() == "[заголовок]") 
                    {
                        isHeader = true;
                        continue;
                    }
                    if (line.ToLower() == "[таблица]")
                    {
                        isBody = true;
                        continue;
                    }
                    var parts = line.Split(';');
                    if (parts.Length < 2) return false;
                    
                    if (parts[0].ToLower().Contains("продавец")
                        || parts[0].ToLower().Contains("номер накладной")
                        || parts[0].ToLower().Contains("дата накладной")
                        || parts[0].ToLower().Contains("номер с/ф")
                        || parts[0].ToLower().Contains("дата с/ф"))
                        counter++;
                }                
            }
            return isHeader & isBody & (counter == 5);
        }
    }
}
