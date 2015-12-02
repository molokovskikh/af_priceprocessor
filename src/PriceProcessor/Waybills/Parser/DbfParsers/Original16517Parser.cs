using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class Original16517Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			var parcer = new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "DATEDOC")
				.DocumentHeader(d => d.ProviderDocumentId, "NDOC")
				.Invoice(i => i.Amount, "SUMDOC")
				.Invoice(i => i.InvoiceDate, "BILLDT")
				.Invoice(i => i.InvoiceNumber, "BILLNUM")
				.Invoice(i => i.RecipientId, "PODRCD")
				.Invoice(i => i.RecipientName, "APTEKA")
				.Line(l => l.Amount, "SUMPAY")
				.Line(l => l.BillOfEntryNumber, "NUMGTD")
				.Line(l => l.CertificateAuthority, "SERTORG")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "SERTGIVE")
				.Line(l => l.CertificatesEndDate, "SERTDATE")
				.Line(l => l.Code, "CODEPST")
				.Line(l => l.Country, "CNTR")
				.Line(l => l.DateOfManufacture, "DATEMADE")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.NdsAmount, "NDSPST")
				.Line(l => l.Period, "GDATE")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE1")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.RegistryCost, "REGPRC")
				.Line(l => l.RegistryDate, "DATEPRC")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.SupplierCost, "PRICE2N")
				.Line(l => l.VitallyImportant, "GNVLS");

			return parcer;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			var result = false;
			if (data.Columns.Contains("SUMDOC")
				&& !data.Columns.Contains("VENDOR")
				&& !data.Columns.Contains("PRICE_MAN"))
				result = true;
			return result;
		}
	}
}