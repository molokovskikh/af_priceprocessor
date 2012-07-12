using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class ASM5836Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NUMDOC")
				.DocumentHeader(h => h.DocumentDate, "DATE")
				.DocumentInvoice(i => i.ShipperInfo, "KONTRAGENT")
				.DocumentInvoice(i => i.ConsigneeInfo, "COMMENT")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Quantity, "KOL")
				.Line(l => l.SupplierCost, "CENA")
				.Line(l => l.Nds, "NDS_TAX")
				.Line(l => l.NdsAmount, "NDS_SUM")
				.Line(l => l.Amount, "SUMMA")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.Certificates, "SERTIFICAT")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.Producer, "MAKER");
		}

		public static bool CheckFileFormat(DataTable data)
        {
            return data.Columns.Contains("NUMDOC")
                && data.Columns.Contains("DATE")
                && data.Columns.Contains("KONTRAGENT")
                && data.Columns.Contains("CODE")
                && data.Columns.Contains("NAME")
                && data.Columns.Contains("KOL")
                && data.Columns.Contains("CENA")
                && data.Columns.Contains("NDS_TAX")
                && data.Columns.Contains("NDS_SUM")
                && data.Columns.Contains("SUMMA")
                && data.Columns.Contains("SERIA")
                && data.Columns.Contains("SERTIFICAT")
                && data.Columns.Contains("GTD")
                && data.Columns.Contains("COUNTRY")
				&& data.Columns.Contains("MAKER")
                && data.Columns.Contains("COMMENT");
        }
	}
}
