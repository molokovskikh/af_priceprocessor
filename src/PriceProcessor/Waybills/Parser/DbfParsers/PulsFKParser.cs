using System.Data;
using NPOI.SS.Formula.Functions;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	//не указывают кодировку по этому приходится задавать явно
	public class PulsFKParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			var parcer = new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "DATEDOC")
				.DocumentHeader(d => d.ProviderDocumentId, "NDOC", "DOCNUMBER")
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

			// http://redmine.analit.net/issues/53523
			if (Data.Columns.Contains("DOCNUMBER") && Data.Columns.Contains("TSUMPAY"))
			{
				parcer = parcer.Invoice(i => i.Amount, "TSUMPAY");
			}

			if (Data.Columns.Contains("SUMNDS10") && Data.Columns.Contains("SUMNDS20"))
			{
				string sumNds10 = "SUMNDS10";
				string sumNds20 = "SUMNDS20";

				string firstSumNds10 = Data.Rows[0][sumNds10]?.ToString();
				bool isInvoiceSumNds10 = true;

				string firstSumNds20 = Data.Rows[0][sumNds20]?.ToString();
				bool isInvoiceSumNds20 = true;

				foreach (DataRow dr in Data.Rows)
				{
					string valueSumNds10 = dr[sumNds10].ToString();
					if (valueSumNds10.Equals(firstSumNds10))
					{
						continue;
					}
					else
					{
						isInvoiceSumNds10 = false;
						break;
					}
						string valueSumNds20 = dr[sumNds20].ToString();

				}

				if (!isInvoiceSumNds10)
				{
					parcer.Invoice(i => i.NDSAmount10 ,   null);
				}
			}
			return parcer;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			if (data.Columns.Contains("ANL"))
				return false;

			if (data.Columns.Contains("DATEDOC")
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