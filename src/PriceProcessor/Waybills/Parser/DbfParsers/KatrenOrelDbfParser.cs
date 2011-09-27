using System.Data;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class KatrenOrelDbfParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "DCODE")
				.DocumentHeader(h => h.DocumentDate, "DATE_DOC")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "PRODUCT")
				.Line(l => l.Producer, "PRODUCER")
				.Line(l => l.Country, "")
				.Line(l => l.ProducerCost, "PRO_NNDS")
				.Line(l => l.SupplierCost, "PRICE_OPL")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE_BASE")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.Period, "GOD", "SROK_S")
				.Line(l => l.RegistryCost, "PRICE_REES")
				.Line(l => l.Certificates, "SERT_N")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.VitallyImportant, "VitImport", "PV")
				.Line(l => l.Nds, "NDS_PR")
				.Line(l => l.NdsAmount, "NDS_SUM")
				.Line(l => l.Amount, "SUM_OPL")				
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.SupplierPriceMarkup, "NC_OPT_PR");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("DCODE") &&
				   data.Columns.Contains("PRICE_BASE") &&
				   data.Columns.Contains("KOLVO") &&
				   (data.Columns.Contains("GOD") || data.Columns.Contains("SROK_S")) &&
				   data.Columns.Contains("NDS_PR") &&
				   data.Columns.Contains("PRO_NNDS");
		}
	}
}
