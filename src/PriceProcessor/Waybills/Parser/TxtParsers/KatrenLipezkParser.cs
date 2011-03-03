using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
    public class KatrenLipezkParser: BaseIndexingParser
    {
        protected override void SetIndexes()
        {
            CodeIndex = 0;
            ProductIndex = 1;
            ProducerIndex = 2;
            CountryIndex = 3;
            QuantityIndex = 4;
            ProducerCostIndex = 5;
            SupplierCostIndex = 6;
            SupplierCostWithoutNdsIndex = 7;
            NdsIndex = 8;
            SupplierPriceMarkupIndex = 9;
            SerialNumberIndex = 11;
            PeriodIndex = 12;
            CertificatesIndex = 14;
            RegistryCostIndex = 18;
            VitallyImportantIndex = 20;
            
        }

        public static bool CheckFileFormat(string file)
        {
            using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
            {
                var headerCaption = reader.ReadLine();
                if (!headerCaption.ToLower().Equals("[header]"))
                    return false;
                var header = reader.ReadLine().Split(';');
                if ((header.Length != 7) || !header[3].ToLower().Contains("зао нпк катрен"))
                    return false;
                var bodyCaption = reader.ReadLine();
                if (!bodyCaption.ToLower().Equals("[body]"))
                    return false;
                var body = reader.ReadLine().Split(';');
                if (GetDecimal(body[6]) == null)
                    return false;
                if (GetDecimal(body[7]) == null)
                    return false;
                if (GetInteger(body[8]) == null)
                    return false;
                if (GetDecimal(body[18]) == null)
                    return false;

            }
            return true;
        }
    }
}
