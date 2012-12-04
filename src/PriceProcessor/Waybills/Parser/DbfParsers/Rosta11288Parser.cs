using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class Rosta11288Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "DATEDOC")
				.DocumentHeader(d => d.ProviderDocumentId, "NDOC")
				.DocumentInvoice(i => i.AmountWithoutNDS, "SUMPAY")
				.DocumentInvoice(i => i.RecipientId, "PODRCD")
				.DocumentInvoice(i => i.InvoiceNumber, "NUMZ")
				.DocumentInvoice(i => i.InvoiceDate, "DATEZ")
				.DocumentInvoice(i => i.Amount, "SUMS0")
				.DocumentInvoice(i => i.NDSAmount, "SUMSNDS")
				.Line(l => l.Code, "CODEPST")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE1")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE2N")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.Period, "GDATE")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Country, "CNTR")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.RegistryCost, "REGPRC")
				.Line(l => l.BillOfEntryNumber, "NUMGTD")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificateAuthority, "SERTORG")
				.Line(l => l.NdsAmount, "SUMNDS10")
				.Line(l => l.Amount, "SUM0")
				.Line(l => l.CertificatesDate, "SERTGIVE");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			if (data.Columns.Contains("NDOC")
				&& data.Columns.Contains("CNTR")
				&& data.Columns.Contains("SERTIF")
				&& data.Columns.Contains("GDATE")
				&& data.Columns.Contains("PRICE2")
				&& data.Columns.Contains("NUMZ")
				&& !data.Columns.Contains("NAMEAPT")
				&& !data.Columns.Contains("SUMITEM")
				&& data.Columns.Contains("SELLERID")) {
				foreach (DataRow row in data.Rows) {
					if (row["SELLERID"].ToString() != "1111")
						return false;
				}
				return true;
			}
			return false;
		}
	}
}
