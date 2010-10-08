using System;
using System.Data;
using System.Linq;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
    public class Moron_SpbParser : IDocumentParser
    {
        public Document Parse(string file, Document document)
        {
            var data = Dbf.Load(file);
            new DbfParser()
                .DocumentHeader(h => h.ProviderDocumentId, "NP")
                .Line(l => l.Product, "NAME")
                .Line(l => l.Nds, "NN")
                .Line(l => l.Quantity, "KOL")
                .Line(l => l.ProducerCost, "PPS")
                .Line(l => l.SupplierCostWithoutNDS, "POT")
                .Line(l => l.SupplierCost, "POS")
                .Line(l => l.RegistryCost, "ST18")
                .Line(l => l.Period, "DT")
                .Line(l => l.SerialNumber, "NUMB")
                .Line(l => l.Country, "CNTR")
                .Line(l => l.Producer, "FACT")
                .Line(l => l.Certificates, "SS")
                .Line(l => l.Code, "CODP")
                .Line(l => l.VitallyImportant, "ST10")
                .ToDocument(document, data);
            return document;
        }

        public static bool CheckFileFormat(DataTable table)
        {
            return table.Columns.Contains("ST18")
                && table.Columns.Contains("ST10")
                && table.Columns.Contains("SS")
                && table.Columns.Contains("CNTR");
        }
    }
}
