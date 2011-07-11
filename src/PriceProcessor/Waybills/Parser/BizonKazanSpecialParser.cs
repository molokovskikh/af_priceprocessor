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
            CertificatesIndex = 12;
            CertificatesDateIndex = 13;
            ProducerCostIndex = 14;
            RegistryCostIndex = 15;
            SupplierCostIndex = 16;
            VitallyImportantIndex = 17;
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
            document.ProviderDocumentId = GetString(parts[ProviderDocumentIdIndex]);
            if (!String.IsNullOrEmpty(parts[DocumentDateIndex]))            
                document.DocumentDate = ParseHelper.GetDateTime(parts[DocumentDateIndex]);

            var docLine = document.NewLine();

            docLine.Product = GetString(parts[ProductIndex]);
            docLine.SerialNumber = GetString(parts[SerialNumberIndex]);
            docLine.Quantity = Convert.ToUInt32(GetDecimal(parts[QuantityIndex]));
            docLine.SupplierCostWithoutNDS = GetDecimal(parts[SupplierCostWithoutNdsIndex]);
            docLine.Nds = (uint?) GetDecimal(parts[NdsIndex]);
            docLine.NdsAmount = GetDecimal(parts[NdsAmountIndex]);
            docLine.Amount = GetDecimal(parts[AmountIndex]);
            docLine.Producer = GetString(parts[ProducerIndex]);
            if (parts.Count() > PeriodIndex)
                docLine.Period = GetString(parts[PeriodIndex]);
            if(parts.Count() > CertificatesIndex)
                docLine.Certificates = GetString(parts[CertificatesIndex]);
            if (parts.Count() > CertificatesDateIndex)
                docLine.CertificatesDate = GetString(parts[CertificatesDateIndex]);
            if (parts.Count() > ProducerCostIndex)
                docLine.ProducerCost = GetDecimal(parts[ProducerCostIndex]);
            if (parts.Count() > RegistryCostIndex)
                docLine.RegistryCost = GetDecimal(parts[RegistryCostIndex]);
            if (parts.Count() > SupplierCostIndex)
                docLine.SupplierCost = GetDecimal(parts[SupplierCostIndex]);
            if (parts.Count() > VitallyImportantIndex)
                docLine.VitallyImportant = GetBool(parts[VitallyImportantIndex]);
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
