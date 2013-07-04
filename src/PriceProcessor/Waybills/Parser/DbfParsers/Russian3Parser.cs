using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class Russian3Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NDOC")
				.DocumentHeader(h => h.DocumentDate, "DATEDOC")
				.DocumentInvoice(i => i.InvoiceNumber, "BILLNUM")
				.DocumentInvoice(i => i.InvoiceDate, "BILLDT")
				.DocumentInvoice(i => i.Amount, "SUMPAY")
				.Line(l => l.Code, "CODEPST")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Country, "CNTR")
				.Line(l => l.ProducerCostWithoutNDS, "PRICEMAN")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE2N")
				.Line(l => l.SupplierCost, "PRICENDS")
				.Line(l => l.RegistryCost, "REGPRC")
				.Line(l => l.RegistryDate, "DATEPRC")
				.Line(l => l.Amount, "SUMS0")
				.Line(l => l.NdsAmount, "SUMSNDS")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.Period, "GDATE")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "SERTDATE")
				.Line(l => l.CertificateAuthority, "SERTORG")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.BillOfEntryNumber, "NUMGTD")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.OrderId, "NUMZ")
				.Line(l => l.SupplierPriceMarkup, "PRCPT");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("PRICEMAN") &&
				data.Columns.Contains("PRICE2N") &&
				data.Columns.Contains("REGPRC") &&
				data.Columns.Contains("CNTR") &&
				data.Columns.Contains("NUMZ") &&
				data.Columns.Contains("SERTDATE");
		}
	}
}