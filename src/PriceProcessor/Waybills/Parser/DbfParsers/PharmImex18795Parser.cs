using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	/// <summary>
	/// SpecialParser, поля PRICE_NAKE и PRICE_MAKE перепутаны
	/// </summary>
	public class PharmImex18795Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			var x =  new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "N_NACL")
				.DocumentHeader(h => h.DocumentDate, "D_NACL")
				.Invoice(i => i.BuyerName, "APTEKA")
				.Invoice(i => i.ShipperInfo, "FILIAL")
				.Invoice(i => i.Amount, "SUMM")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FACTORY")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.Quantity, "QUANTITY")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE_NAKE")
				.Line(l => l.SupplierCost, "PRICE_MAKE")
				.Line(l => l.Nds, "NDS_PR")
				.Line(l => l.RegistryCost, "PRICE_REES")
				.Line(l => l.RegistryDate, "DATE_REES")
				.Line(l => l.VitallyImportant, "ISLIFE")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.Period, "DATE_VALID")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.NdsAmount, "NDS_SUM")
				.Line(l => l.Amount, "SUM_SNDS")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.EAN13, "SCANCOD")
				.Line(l => l.CertificateAuthority, "ORGAN");
			return x;
		}

		public static bool CheckFileFormat(DataTable table)
		{
			var Index = table.Columns.Contains("N_NACL")
				&& table.Columns.Contains("D_NACL")
				&& table.Columns.Contains("SUM_NAKED")
				&& table.Columns.Contains("CENA_RT") ;
			return Index;
		}
	}
}