using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class KatrenVolgogradParser2 : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NDOC")
				.DocumentHeader(d => d.DocumentDate, "DATEDOC")
				.DocumentInvoice(i => i.InvoiceNumber, "BILLNUM")
				.DocumentInvoice(i => i.InvoiceDate, "BILLDT")
				.DocumentInvoice(i => i.NDSAmount, "SUMSNDS")
				.DocumentInvoice(i => i.RecipientAddress, "PUNKT")
				.DocumentInvoice(i => i.RecipientId, "PODRCD")
				.DocumentInvoice(i => i.Amount, "TotalSUM")
				.DocumentInvoice(i => i.NDSAmount10, "TSUMNDS10")
				.DocumentInvoice(i => i.NDSAmount18, "TSUMNDS18")
				.DocumentInvoice(i => i.AmountWithoutNDS10, "TotlSUM10")
				.DocumentInvoice(i => i.AmountWithoutNDS18, "TotlSUM18")
				.DocumentInvoice(i => i.AmountWithoutNDS0, "TotalSUM0")
				.Line(l => l.Code, "GVId")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Country, "CNTRMADE")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE1N")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE2N")
				.Line(l => l.SupplierCost, "PRICENDS")
				.Line(l => l.SupplierPriceMarkup, "PRCOPT")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.Amount, "SUMPAY")
				.Line(l => l.NdsAmount, "SUMNDS")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.BillOfEntryNumber, "NUMGTD")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.Period, "GDATE")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificateAuthority, "SERTORG")
				.Line(l => l.CertificatesDate, "SERTDATE")
				.Line(l => l.OrderId, "NUMZ")
				.Line(l => l.RegistryCost, "REGPRC")
				.Line(l => l.CodeCr, "CODEPST")
				.Line(l => l.RegistryDate, "DATEPRC");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("PRCOPT")
				&& data.Columns.Contains("GNVLS")
				&& data.Columns.Contains("SUMS1")
				&& data.Columns.Contains("PUNKT")
				&& data.Columns.Contains("SUMS0");
		}
	}
}