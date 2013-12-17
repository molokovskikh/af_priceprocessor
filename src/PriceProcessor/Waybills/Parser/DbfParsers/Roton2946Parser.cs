using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class Roton2946Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NUM_DOC")
				.DocumentHeader(h => h.DocumentDate, "DATE_DOC")
				.Invoice(i => i.InvoiceNumber, "NUM_SF")
				.Invoice(i => i.InvoiceDate, "DATE_SF")
				.Invoice(i => i.ShipperInfo, "ORG")
				.Invoice(i => i.BuyerName, "POLUCH")
				.Line(l => l.Code, "CODE_TOVAR")
				.Line(l => l.Product, "NAME_TOVAR")
				.Line(l => l.Producer, "PROIZ")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.Quantity, "VOLUME")
				.Line(l => l.Nds, "PCT_NDS")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE")
				.Line(l => l.SupplierCost, "PRICE_NDS")
				.Line(l => l.Amount, "SUM_B_NDS")
				.Line(l => l.NdsAmount, "SUMMA_NDS")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.Period, "SROK")
				.Line(l => l.Certificates, "CER_NUMBER")
				.Line(l => l.CertificateAuthority, "SERT_ORG");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("SUM_B_NDS")
				&& data.Columns.Contains("EAN13")
				&& data.Columns.Contains("DATE_SF");
		}
	}
}
