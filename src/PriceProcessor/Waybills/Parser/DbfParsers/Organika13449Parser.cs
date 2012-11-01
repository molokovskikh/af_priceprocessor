using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class Organika13449Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NUMDOC")
				.DocumentHeader(h => h.DocumentDate, "DATEDOC")
				.DocumentInvoice(i => i.Amount, "SUMDOC")
				.DocumentInvoice(i => i.RecipientName, "APTEKA")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "MANUFACT")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.Quantity, "COUNT")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE_MAN")
				.Line(l => l.SupplierPriceMarkup, "MARGIN")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE")
				.Line(l => l.Amount, "SUM")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.Certificates, "CERT")
				.Line(l => l.Period, "TERM")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.EAN13, "BARCODE")
				.Line(l => l.VitallyImportant, "GNVLS");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("PRICE_MAN")
				&& data.Columns.Contains("TERM")
				&& data.Columns.Contains("MANUFACT");
		}
	}
}
