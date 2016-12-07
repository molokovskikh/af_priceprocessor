using System.Data;
using NPOI.SS.Formula.Functions;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class GrandCapitalVlg19513Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			var parcer = new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NDOC")
				.DocumentHeader(d => d.DocumentDate, "DATEDOC")
				.Invoice(i => i.RecipientId, "PODRCD")
				.Invoice(i => i.RecipientAddress, "PODADR")
				.Invoice(i => i.SellerName, "DISTRIB")
				.Invoice(i => i.Amount, "TSUMPAY")
				.Invoice(i => i.NDSAmount, "TSUMNDS")
				.Invoice(i => i.NDSAmount10, "TSUMNDS10")
				.Invoice(i => i.NDSAmount18, "TSUMNDS20")
				.Invoice(i => i.AmountWithoutNDS10, "TSUM10")
				.Invoice(i => i.AmountWithoutNDS18, "TSUM20")
				.Invoice(i => i.AmountWithoutNDS0, "TSUM0")
				.Line(l => l.Code, "CODEPST")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE1")
				.Line(l => l.SupplierCost, "PRICE2")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE2N")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.Period, "GDATE")
				.Line(l => l.DateOfManufacture, "DateMade")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Country, "CNTR")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.RegistryCost, "REGPRC")
				.Line(l => l.RegistryDate, "DATEPRC")
				.Line(l => l.BillOfEntryNumber, "NUMGTD")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "SERTDATE")
				.Line(l => l.CertificateAuthority, "SERTORG")
				.Line(l => l.Amount, "SUMPAY")
				.Line(l => l.NdsAmount, "SUMNDS")
				.Line(l => l.OrderId, "NUMZ");

			return parcer;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("DateMade")
				&& data.Columns.Contains("QNTPACK")
				&& data.Columns.Contains("EXCHCODE")
				&& data.Columns.Contains("TSUMPAY")
				&& data.Columns.Contains("FLAG");
		}
	}
}