using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class BaltimorYoshkarOlaParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NOM_NAKL")
				.DocumentHeader(d => d.DocumentDate, "DATA_SHET")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "ZAVOD")
				.Line(l => l.Country, "STRANA")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.Period, "SROK")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.SupplierPriceMarkup, "OPT_NAC")
				.Line(l => l.SupplierCost, "CENA_OTP")
				.Line(l => l.SupplierCostWithoutNDS, "CENA_BNDS")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.NdsAmount, "SUMNDS")
				.Line(l => l.Amount, "SUMSNDS")
				.Line(l => l.EAN13, "BAR")
				.Line(l => l.ProducerCostWithoutNDS, "CIZG")
				.Line(l => l.VitallyImportant, "JV")
				.Line(l => l.RegistryCost, "GOSREESTR");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("KOD")
				&& data.Columns.Contains("NAME")
				&& data.Columns.Contains("ZAVOD")
				&& data.Columns.Contains("STRANA")
				&& data.Columns.Contains("GTD")
				&& data.Columns.Contains("SROK")
				&& data.Columns.Contains("KOLVO")
				&& data.Columns.Contains("CENA_OTP")
				&& data.Columns.Contains("CENA_BNDS")
				&& data.Columns.Contains("GOSREESTR")
				&& data.Columns.Contains("CIZG")
				&& data.Columns.Contains("JV");
		}
	}
}