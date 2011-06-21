using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
    public class ProfitmedSpbParser : BaseDbfParser
    {
        public override DbfParser GetParser()
        {
            return new DbfParser()
                .DocumentHeader(d => d.ProviderDocumentId, "N_NAKL")
                .DocumentHeader(d => d.DocumentDate, "D_NAKL")
                .Line(l => l.Code, "CODE")
                .Line(l => l.Product, "NAME")
                .Line(l => l.Producer, "FACTORY")
                .Line(l => l.Country, "COUNTRY")
                .Line(l => l.Quantity, "QUANTITY")
                .Line(l => l.Nds, "NDS_PR")
                .Line(l => l.SupplierCostWithoutNDS, "PRICE")
                .Line(l => l.SupplierCost, "PRICE_N")
                .Line(l => l.ProducerCost, "PRICEIZG")
                .Line(l => l.NdsAmount, "NDS_SUM")
                .Line(l => l.Amount, "SUM_VSEGO")
                .Line(l => l.SerialNumber, "SERIES")
                .Line(l => l.Certificates, "SERT")
                .Line(l => l.Period, "DATE_VALID");
        }

        public static bool CheckFileFormat(DataTable data)
        {
            return data.Columns.Contains("N_NAKL")
                && data.Columns.Contains("D_NAKL")
                && data.Columns.Contains("CODE")
                && data.Columns.Contains("NAME")
                && data.Columns.Contains("FACTORY")
                && data.Columns.Contains("COUNTRY")
                && data.Columns.Contains("QUANTITY")
                && data.Columns.Contains("NDS_PR")
                && data.Columns.Contains("PRICE")
                && data.Columns.Contains("PRICE_N")
                && data.Columns.Contains("PRICEIZG")
                && data.Columns.Contains("NDS_SUM")
                && data.Columns.Contains("SUM_VSEGO")
                && data.Columns.Contains("DATE_VALID");
        }
    }
}
