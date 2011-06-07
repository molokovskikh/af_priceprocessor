using System.Data;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
    public class AlphaMedicaKazanParser : BaseDbfParser
    {
        public override Document Parse(string file, Document document)
        {
            Encdoing = Encoding.GetEncoding(866);
            return base.Parse(file, document);
        }

        public override DbfParser GetParser()
        {
            return new DbfParser()
                .DocumentHeader(d => d.ProviderDocumentId, "NUMDOC")
                .DocumentHeader(d => d.DocumentDate, "DATE")
                .Line(l => l.Code, "CODE")
                .Line(l => l.Product, "NAME")
                .Line(l => l.Quantity, "KOL")
                .Line(l => l.SupplierCostWithoutNDS, "CENA0")
                .Line(l => l.SupplierCost, "CENA")
                .Line(l => l.Amount, "SUMMA")
                .Line(l => l.Nds, "NDS_TAX")
                .Line(l => l.NdsAmount, "SUM_NDS")
                .Line(l => l.SerialNumber, "SERIA")
                .Line(l => l.Country, "COUNTRY");
        }

        public static bool CheckFileFormat(DataTable data)
        {
            return
                data.Columns.Contains("NUMDOC") &&
                data.Columns.Contains("DATE") &&
                data.Columns.Contains("CODE") &&
                data.Columns.Contains("NAME") &&
                data.Columns.Contains("CENA0") &&
                data.Columns.Contains("CENA") &&
                data.Columns.Contains("SUMMA") &&
                data.Columns.Contains("SUM_NDS");
        }
    }
}
