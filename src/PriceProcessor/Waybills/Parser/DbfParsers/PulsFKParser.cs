using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	//не указывают кодировку по этому приходится задавать явно
	public class PulsFKParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			var parcer = new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "DATEDOC")
				.DocumentHeader(d => d.ProviderDocumentId, "NDOC")
				.DocumentInvoice(i => i.InvoiceNumber, "BILLNUM")
				.DocumentInvoice(i => i.InvoiceDate, "BILLDT")
				.DocumentInvoice(i => i.AmountWithoutNDS, "SUMPAY")
				.DocumentInvoice(i => i.RecipientAddress, "PODRCD")

				.Line(l => l.Code, "CODEPST")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Country, "CNTR")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE2N")
				.Line(l => l.SupplierCost, "PRICE2")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.Amount, "SUMS0")
				.Line(l => l.NdsAmount, "SUMSNDS")
				.Line(l => l.Period, "GDATE")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.RegistryCost, "REGPRC")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.SupplierPriceMarkup, "PROCNDB")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.CertificateAuthority, "SERTORG")
				.Line(l => l.CertificatesDate, "SERTGIVE", "SERTDATE")
				.Line(l => l.OrderId, "NUMZ")
				.Line(l => l.BillOfEntryNumber, "NUMGTD");

			if (!Data.Columns.Contains("PRICE1N")) {
				parcer = parcer.Line(l => l.ProducerCostWithoutNDS, "MAKERPRICE", "PRICE1");
			}
			else {
				parcer = parcer.Line(l => l.ProducerCost, "PRICE1")
					.Line(l => l.ProducerCostWithoutNDS, "PRICE1N");
			}

			return parcer;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			if(data.Columns.Contains("NDOC")
				&& data.Columns.Contains("CNTR")
				&& data.Columns.Contains("SERTIF")
				&& data.Columns.Contains("GDATE")
				&& data.Columns.Contains("PRICE2")
				&& data.Columns.Contains("NUMZ")
				&& !data.Columns.Contains("NAMEAPT")
				&& !data.Columns.Contains("SUMITEM")) {
				if(data.Columns.Contains("SELLERID")) {
					foreach (DataRow row in data.Rows) {
						if (row["SELLERID"].ToString() != "1111")
							return true;
					}
					return false;
				}
				return true;
			}
			return false;
		}
	}
}