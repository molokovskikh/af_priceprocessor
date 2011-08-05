﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Parser.TxtParsers;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
    public class FarmPartnerKalugaParser : BaseIndexingParser
    {
        protected override void SetIndexes()
        {
            ProviderDocumentIdIndex = 0;
            DocumentDateIndex = 1;

            CodeIndex = 0;
            ProductIndex = 1;
            ProducerIndex = 2;
            CountryIndex = 3;
            QuantityIndex = 4;            
            SupplierCostWithoutNdsIndex = 7;
            SupplierCostIndex = 8;
            PeriodIndex = 10;
            BillOfEntryNumberIndex = 11;
            CertificatesIndex = 12;
            SerialNumberIndex = 13;
            EAN13Index = 16;
            RegistryCostIndex = 18;
            VitallyImportantIndex = 20;
            NdsAmountIndex = 21;
        }

        public static bool CheckFileFormat(string file)
        {
            using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
            {
                var headerCaption = reader.ReadLine();
                if (!headerCaption.ToLower().Equals("[header]"))
                    return false;
                var header = reader.ReadLine().Split(';');
                if (header.Length != 16 || !header[10].ToLower().Contains("фармпартнер")) return false;
                var bodyCaption = reader.ReadLine();
                string line = reader.ReadLine();
                while(line != null)
                {
                    if (line.ToLower().Equals("[body]"))
                        break;
                    line = reader.ReadLine();
                }
                if (line == null)
                    return false;
                var body = reader.ReadLine().Split(';');
                if (body.Length != 22) return false;
                if (GetDecimal(body[7]) == null || GetDecimal(body[8]) == null) return false;
            }
            return true;
        }
    }
}