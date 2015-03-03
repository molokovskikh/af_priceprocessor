using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	//не указывают кодировку по этому приходится задавать явно
	public class VolgofarmParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			var parcer = new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NDOC")
				.DocumentHeader(d => d.DocumentDate, "DATEDOC")

				.Invoice(i => i.Amount, "SUMPAY")
				.Invoice(i => i.NDSAmount10, "SUMNDS10")
				.Invoice(i => i.NDSAmount18, "SUMNDS20")
				.Invoice(i => i.AmountWithoutNDS10, "SUM10")
				.Invoice(i => i.AmountWithoutNDS18, "SUM20")
				.Invoice(i => i.AmountWithoutNDS0, "SUM0")
				.Invoice(i => i.RecipientAddress, "PODRCD")

				.Line(l => l.Code, "CODEPST")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Country, "CNTR")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE2N")
				.Line(l => l.SupplierCost, "PRICE2")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.Period, "GDATE")
				.Line(l => l.RegistryCost, "REGPRC")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificateAuthority, "SERTORG")
				.Line(l => l.CertificatesDate, "SERTGIVE")
				.Line(l => l.OrderId, "NUMZ")
				.Line(l => l.BillOfEntryNumber, "NUMGTD")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE1")
				.Line(l => l.Amount, "SUMPAY");											// поле "SUMPAY" стали использовать вместо "SUMS0"

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
				&& !data.Columns.Contains("SUMITEM")
				&& data.Columns.Contains("UNITNAME")) {
				return true;
			}
			return false;
		}
	}
}
