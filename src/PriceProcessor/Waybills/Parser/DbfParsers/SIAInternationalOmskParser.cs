using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
    public class SiaInternationalOmskParser : BaseDbfParser
    {
        public override DbfParser GetParser()
        {
            return new DbfParser()
                //.DocumentHeader(h => h.ProviderDocumentId, "")
                //.DocumentHeader(h => h.DocumentDate, "")
                .Line(l => l.Code, "KOD")
                .Line(l => l.Product, "NAME")
                .Line(l => l.Producer, "PROIZV")
                .Line(l => l.Country, "COUNTRY")
                .Line(l => l.ProducerCost, "CENAPROIZ")
                .Line(l => l.SupplierCost, "CENASNDS")
                .Line(l => l.SupplierCostWithoutNDS, "CENABNDS")
                .Line(l => l.Quantity, "KOLVO")
                .Line(l => l.Period, "DATAEND")
                .Line(l => l.RegistryCost, "REESTR")
                .Line(l => l.Certificates, "SERTIF")
                .Line(l => l.SerialNumber, "SERII")
                .Line(l => l.Nds, "NDSPOSTAV");
        }


        public static bool CheckFileFormat(DataTable data) {
            return data.Columns.Contains("NAME") &&
                   data.Columns.Contains("PROIZV") &&
                   data.Columns.Contains("KOLVO") &&
                   data.Columns.Contains("CENAPROIZV") &&
                   data.Columns.Contains("SUMMANDS") &&
                   data.Columns.Contains("COUNTRY");
        }
    }
}