using System.Data;
using System.Text;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
    public class SiaInternationalOmskPParser : IDocumentParser
    {
        protected Encoding Encoding = Encoding.GetEncoding(866);

        public Document Parse(string file, Document document) {
            var data = Dbf.Load(file, Encoding);

            new DbfParser()
                .DocumentHeader(h => h.ProviderDocumentId, "NN")
                .DocumentHeader(h => h.DocumentDate, "")
                .Line(l => l.Code, "KOD")
                .Line(l => l.Product, "NAME")
                .Line(l => l.Producer, "PROIZV")
                .Line(l => l.Country, "COUNTRY")
                .Line(l => l.ProducerCost, "PLT_NO_NDS") // Изменить
                .Line(l => l.SupplierCost, "CENAPROIZ")
                .Line(l => l.SupplierCostWithoutNDS, "CENABNDS")
                .Line(l => l.Quantity, "KOLVO")
                .Line(l => l.Period, "ACVALDATE") // Изменить
                .Line(l => l.RegistryCost, "REESTR")
                .Line(l => l.Certificates, "SERTIF")
                .Line(l => l.SerialNumber, "SERII")
                .Line(l => l.VitallyImportant, "")
                .Line(l => l.Nds, "SUMMANDS")
                .ToDocument(document, data);

            return document;
        }

        public static bool CheckFileFormat(DataTable data) {
            return data.Columns.Contains("ACWARES") &&
                   data.Columns.Contains("WARESNAME") &&
                   data.Columns.Contains("ACAMOUNT") &&
                   data.Columns.Contains("PLT_NO_NDS") &&
                   data.Columns.Contains("NDOCNUM") &&
                   data.Columns.Contains("COUNTRY");
        }
    }
}