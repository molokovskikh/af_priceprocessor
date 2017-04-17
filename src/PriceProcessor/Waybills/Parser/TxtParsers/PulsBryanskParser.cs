using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser.TxtParsers;

namespace Inforoom.PriceProcessor.Waybills.Parser.SstParsers
{
    public class PulsBryanskParser : IDocumentParser
    {
        public Document Parse(string file, Document document)
        {
            using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
            {
                string readLine;
                var startReading = false;
                while ((readLine = reader.ReadLine()) != null)
                {

                    if (readLine.Trim() == "[Header]")
                    {
                        readLine = reader.ReadLine();
                        var split = readLine.Split(';');
                        document.ProviderDocumentId = split[0];
                        document.DocumentDate = NullableConvert.ToDateTime(split[1]);
                        startReading = false;
                    }

                    if (readLine.Trim() == "[Body]")
                    {
                        startReading = true;
                        continue;
                    }

                    if (!startReading)
                        continue;

                    var sstParser = readLine.Split(';');
                        var line = document.NewLine();
                        line.Code = sstParser[0];
                        line.Product = sstParser[1];
                        line.Producer = sstParser[2];
                        line.Country = sstParser[3];
                        line.Quantity = (uint?)NullableConvert.ToFloatInvariant(sstParser[4]);
                        line.ProducerCostWithoutNDS = NullableConvert.ToDecimal(sstParser[6], CultureInfo.InvariantCulture);
                        line.SupplierCostWithoutNDS = NullableConvert.ToDecimal(sstParser[7], CultureInfo.InvariantCulture);
                        line.Nds = (uint?)NullableConvert.ToFloatInvariant(sstParser[26]);
                        line.NdsAmount = (uint?)NullableConvert.ToFloatInvariant(sstParser[28]);
                        line.BillOfEntryNumber = sstParser[11];
                        line.Certificates = sstParser[12];
                        line.EAN13 = NullableConvert.ToUInt64(sstParser[16]);
                        line.Period = sstParser[15];
                        line.ProducerCost = NullableConvert.ToDecimal(sstParser[5], CultureInfo.InvariantCulture);

                    if (sstParser[25].Contains("0"))
                        line.VitallyImportant = false;
                    else if(sstParser[25].Contains("1"))
                        line.VitallyImportant = true;
                }
            }
            return document;
        }



        public static bool CheckFileFormat(string file)
        {
            using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
            {
                var header = reader.ReadLine().Split(';');
                if (header.Length != 20)
                    return false;
                var headerCaption = reader.ReadLine();
                if (!headerCaption.ToLower().Equals("[header]"))
                    return false;
                var header2 = reader.ReadLine().Split(';');
                if (header2.Length != 19)
                    return false;
                var header3 = reader.ReadLine().Split(';');
                if (header3.Length != 32)
                    return false;
                var bodyCaption = reader.ReadLine();
                if (!bodyCaption.ToLower().Equals("[body]"))
                    return false;
            }
            return true;
        }
    }
}
