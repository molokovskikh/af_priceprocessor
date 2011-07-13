using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
    public class AptekaHoldingCheboksaryParser : BaseDbfParser
    {
        public override DbfParser GetParser()
        {
            return new DbfParser()
                .DocumentHeader(d => d.ProviderDocumentId, "NUM_DOC")
                .DocumentHeader(d => d.DocumentDate, "DATE_DOC")
                .Line(l => l.Code, "KOD")
                .Line(l => l.Product, "NAME_TOVAR")
                .Line(l => l.Producer, "PROIZ")
                .Line(l => l.Country, "COUNTRY")
                .Line(l => l.BillOfEntryNumber, "GTD")
                .Line(l => l.Quantity, "VOLUME")
                .Line(l => l.ProducerCost, "PR_PROIZ")
                .Line(l => l.SupplierPriceMarkup, "NAC_PROC")
                .Line(l => l.Nds, "PCT_NDS")
                .Line(l => l.NdsAmount, "SUMMA_NDS")
                .Line(l => l.SupplierCost, "PRICE")
                .Line(l => l.SupplierCostWithoutNDS, "PR_BNDS")
                .Line(l => l.Amount, "SUMMA")
                .Line(l => l.SerialNumber, "SERIA")
                .Line(l => l.Certificates, "DOCUMENT")
                .Line(l => l.CertificatesDate, "SERTDATE")
                .Line(l => l.Period, "SROK")
                .Line(l => l.RegistryCost, "PR_REESTR")
                .Line(l => l.VitallyImportant, "GNVLS");
        }

        public static bool CheckFileFormat(DataTable data)
        {
            return data.Columns.Contains("KOD") &&
                data.Columns.Contains("NUM_DOC") &&
                data.Columns.Contains("DATE_DOC") &&
                data.Columns.Contains("NAME_TOVAR") &&
                data.Columns.Contains("PROIZ") &&
                data.Columns.Contains("GTD") &&
                data.Columns.Contains("VOLUME") &&
                data.Columns.Contains("PR_PROIZ") &&
                data.Columns.Contains("PRICE") &&
                data.Columns.Contains("PR_BNDS") &&
                data.Columns.Contains("GNVLS") &&
                data.Columns.Contains("PCT_NDS");
        }
    }
}
