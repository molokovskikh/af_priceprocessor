using System;
using System.IO;
using System.Linq;
using Inforoom.PriceProcessor.Waybills.Parser.Helpers;
using Inforoom.PriceProcessor.Waybills.Parser.TxtParsers;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
    public class BizonKazanSpecialParser : BaseIndexingParser
    {
        protected override void SetIndexes()
        {
            DocumentDateIndex = 1;
            ProviderDocumentIdIndex = 2;
            ProductIndex = 3;
            SerialNumberIndex = 4;
            QuantityIndex = 5;
            SupplierCostWithoutNdsIndex = 6;
            NdsIndex = 7;
            NdsAmountIndex = 8;
            AmountIndex = 9;
            ProducerIndex = 10;
            PeriodIndex = 11;            
        }

        public static bool CheckFileFormat(string file)
        {
            if (Path.GetExtension(file).ToLower() != ".txt")
                return false;
            using (var parser = new HeaderBodyParser(file, String.Empty))
            {
                var header = parser.Lines().Where(l => l.StartsWith("[header]") || l.StartsWith("[body]"));
                if (header.Count() > 0)
                    return false;                
            }
            return true;
        }

        protected void ReadLine(Document document, string line)
        {
            var parts = line.Split(';');
            document.ProviderDocumentId = parts[ProviderDocumentIdIndex];
            if (!String.IsNullOrEmpty(parts[DocumentDateIndex]))            
                document.DocumentDate = ParseHelper.GetDateTime(parts[DocumentDateIndex]);

            var docLine = document.NewLine();

            docLine.Product = parts[ProductIndex];
            docLine.SerialNumber = parts[SerialNumberIndex];
            docLine.Quantity = Convert.ToUInt32(GetDecimal(parts[QuantityIndex]));
            docLine.SupplierCostWithoutNDS = GetDecimal(parts[SupplierCostWithoutNdsIndex]);
            docLine.Nds = (uint?) GetDecimal(parts[NdsIndex]);
            docLine.NdsAmount = GetDecimal(parts[NdsAmountIndex]);
            docLine.Amount = GetDecimal(parts[AmountIndex]);
            docLine.Producer = parts[ProducerIndex];
            if (parts.Count() > PeriodIndex)
                docLine.Period = GetString(parts[PeriodIndex]);            
            docLine.SetValues();
        }

        public override Document Parse(string file, Document document)
        {
            SetIndexes();

            using (var parser = new HeaderBodyParser(file, CommentMark))
            {
                foreach (var line in parser.Lines())
                {
                    ReadLine(document, line);
                }
            }
            return document;
        }
    }
}
