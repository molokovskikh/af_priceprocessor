using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class SiaVrnParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NUM_DOC")
				.DocumentHeader(d => d.DocumentDate, "DATE_DOC")
				.Line(l => l.Code, "ID_TOVAR")
				.Line(l => l.Product, "NAME_TOVAR")
				.Line(l => l.CodeCr, "ID_MAKER")
				.Line(l => l.Producer, "MAKER")
				.Line(l => l.CountryCode, "ID_COUNTRY")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.Quantity, "AMOUNT")
				.Line(l => l.Nds, "PCT_NDS")
				.Line(l => l.ProducerCostWithoutNDS, "MAKER_PRC")
				.Line(l => l.SupplierPriceMarkup, "OPT_PCT")
				.Line(l => l.SupplierCostWithoutNDS, "OPT_PRICE")
				.Line(l => l.SerialNumber, "SERIE")
				.Line(l => l.CertificatesDate, "SEREXPDATE")
				.Line(l => l.Certificates, "SERT_INFO")
				.Line(l => l.CertificateAuthority, "SERTORGAN");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("PCT_NDS")
				&& data.Columns.Contains("MAKER_PRC")
				&& data.Columns.Contains("OPT_PCT")
				&& data.Columns.Contains("OPT_PRICE");
		}
	}
}