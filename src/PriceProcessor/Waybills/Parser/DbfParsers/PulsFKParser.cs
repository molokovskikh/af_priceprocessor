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
				.Invoice(i => i.InvoiceNumber, "BILLNUM")
				.Invoice(i => i.InvoiceDate, "BILLDT")
				.Invoice(i => i.AmountWithoutNDS, "SUMPAY")
				.Invoice(i => i.NDSAmount10, "SUMNDS10")
				.Invoice(i => i.NDSAmount18, "SUMNDS20")
				.Invoice(i => i.AmountWithoutNDS10, "SUM10")
				.Invoice(i => i.AmountWithoutNDS18, "SUM20")
				.Invoice(i => i.AmountWithoutNDS0, "SUM0")
				.Invoice(i => i.Amount, "SUMPAY")
				.Line(l => l.Code, "CODEPST")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Country, "CNTR")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE2N")
				.Line(l => l.SupplierCost, "PRICE2")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.Nds, "NDS")
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
				.Line(l => l.CountryCode, "cntrcode")
				.Line(l => l.UnitCode, "unitcode")
				.Line(l => l.BillOfEntryNumber, "NUMGTD");

			if (!Data.Columns.Contains("ADRPOL")) {
				if (!Data.Columns.Contains("PRICE1N") && !Data.Columns.Contains("PRICEMAN")) {
					parcer = parcer.Line(l => l.ProducerCostWithoutNDS, "MAKERPRICE", "PRICE1");
				}
				else if (Data.Columns.Contains("PRICE1N")) {
					parcer = parcer.Line(l => l.ProducerCost, "PRICE1")
						.Line(l => l.ProducerCostWithoutNDS, "PRICE1N");
				}
				else {
					parcer = parcer.Line(l => l.ProducerCost, "PRICE1")
						.Line(l => l.ProducerCostWithoutNDS, "PRICEMAN");
				}
				parcer
					.Line(l => l.NdsAmount, "SUMSNDS")
					.Line(l => l.Amount, "SUMS0");
			}
			else {
				parcer = parcer.Line(l => l.ProducerCost, "PRICE1N")
					.Line(l => l.Amount, "SUMSNDS")
					.Line(l => l.ProducerCostWithoutNDS, "PRICE1");
			}

			if (!Data.Columns.Contains("ADRPOL")) {
				if (Data.Columns.Contains("PRICEMAN"))
					parcer = parcer.Invoice(i => i.RecipientId, "PODRCD");
				else
					parcer = parcer.Invoice(i => i.RecipientAddress, "PODRCD");
			}
			else {
				parcer = parcer.Invoice(i => i.RecipientAddress, "ADRPOL");
			}

			return parcer;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			if (data.Columns.Contains("ANL"))
				return false;

			if (data.Columns.Contains("NDOC")
				&& data.Columns.Contains("CNTR")
				&& data.Columns.Contains("SERTIF")
				&& data.Columns.Contains("GDATE")
				&& data.Columns.Contains("PRICE2")
				&& data.Columns.Contains("NUMZ")
				&& !data.Columns.Contains("NAMEAPT")
				&& !data.Columns.Contains("SUMITEM")
				&& !data.Columns.Contains("UNITNAME")) {
				if (data.Columns.Contains("SELLERID")) {
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