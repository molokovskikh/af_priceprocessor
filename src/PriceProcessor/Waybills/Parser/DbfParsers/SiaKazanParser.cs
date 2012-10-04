using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class SiaKazanParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "DATA_NAKL")
				.DocumentHeader(d => d.ProviderDocumentId, "NUM_NAKL")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.SupplierCostWithoutNDS, "CENABNDS")
				.Line(l => l.SupplierCost, "CENASNDS")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.Nds, "NDSPOSTAV")
				.Line(l => l.ProducerCostWithoutNDS, "CENAPROIZ")
				.Line(l => l.Period, "SROK_GODN", "DATAEND")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "DATAREGSE")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.SerialNumber, "SERII")
				.Line(l => l.NdsAmount, "SUMMANDS")

				.Line(l => l.EAN13, "SHTRIHKOD")
				.Line(l => l.Amount, "SUMMA")
				.Line(l => l.BillOfEntryNumber, "N_DECLAR")
				.DocumentInvoice(i => i.BuyerName, "APTEKA")
				.DocumentInvoice(i => i.RecipientAddress, "ADDRESS");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("KOD")
				&& data.Columns.Contains("NAME")
				&& data.Columns.Contains("PROIZV")
				&& data.Columns.Contains("COUNTRY")
				&& data.Columns.Contains("SERTIF")
				&& data.Columns.Contains("CENABNDS")
				&& data.Columns.Contains("CENASNDS")
				&& (data.Columns.Contains("SROK_GODN") || data.Columns.Contains("DATAEND"))
				&& data.Columns.Contains("PROIZV");
		}
	}
}