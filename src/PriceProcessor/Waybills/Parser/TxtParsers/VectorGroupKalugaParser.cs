using System;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
    public class VectorGroupKalugaParser : BaseIndexingParser
    {
        protected override void SetIndexes()
        {
            CodeIndex = -1;
            ProductIndex = 1;
            QuantityIndex = 3;
            ProducerIndex = 4;
            SupplierCostWithoutNdsIndex = 5;
            SupplierCostIndex = 6;
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
            CertificatesDateIndex = 21;
            VitallyImportantIndex = 22;
        }

        public override Document Parse(string file, Document document)
        {
            SetIndexes();
            using (var parser = new HeaderBodyParser(file, CommentMark))
            {                
                foreach (var headline in parser.Header())
                {                    
                    var parts = headline.Split(';');
                    if (parts[0].ToLower() == "номер накладной")
                        document.ProviderDocumentId = parts[1];
                    if (parts[0].ToLower() == "дата накладной")
                        if (!String.IsNullOrEmpty(parts[1]))
                            document.DocumentDate = Convert.ToDateTime(parts[1]);
                    if (document.ProviderDocumentId != null && document.DocumentDate != null) break;
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
            bool isVectorGroup = false;
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
                    
                    if (parts[0].ToLower().Contains("продавец") && parts[1].ToLower().Contains("\"вектор групп\""))
                    {
                        isVectorGroup = true;
                        continue;
                    }                    
                }                
            }
            return isHeader & isBody & isVectorGroup;
        }
    }
}
