using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class OriolaTula4013Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "DATEDOC")
				.DocumentHeader(d => d.ProviderDocumentId, "NDOC")
				.DocumentInvoice(i => i.InvoiceNumber, "BILLNUM")
				.DocumentInvoice(i => i.InvoiceDate, "BILLDT")
				.DocumentInvoice(i => i.AmountWithoutNDS, "SUMPAY")
				.DocumentInvoice(i => i.RecipientAddress, "PODRCD")
				.DocumentInvoice(i => i.AmountWithoutNDS18, "SUM20")
				.DocumentInvoice(i => i.AmountWithoutNDS10, "SUM10")
				.DocumentInvoice(i => i.AmountWithoutNDS0, "SUM0")
				.DocumentInvoice(i => i.RecipientId, "PODRCD")
				.DocumentInvoice(i => i.Amount, "SUMPAY")
				.Line(l => l.Code, "CODEPST")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Country, "CNTR")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE2N")
				.Line(l => l.SupplierCost, "PRICE2")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE1N")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.Amount, "SUMS0")
				.Line(l => l.NdsAmount, "SUMSNDS")
				.Line(l => l.Period, "GDATE")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.RegistryCost, "REGPRC")
				.Line(l => l.RegistryDate, "DATEPRC")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.SupplierPriceMarkup, "PRCOPT")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.CertificateAuthority, "SERTORG")
				.Line(l => l.CertificatesDate, "SERTGIVE")
				.Line(l => l.OrderId, "NUMZ")
				.Line(l => l.BillOfEntryNumber, "NUMGTD");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NDOC")
				&& data.Columns.Contains("CNTR")
				&& data.Columns.Contains("SERTIF")
				&& data.Columns.Contains("NAMEAPT")
				&& data.Columns.Contains("PRICE2")
				&& data.Columns.Contains("NUMZ")
				&& data.Columns.Contains("PRCOPT")
				&& !data.Columns.Contains("SUMITEM");
		}
	}
}
