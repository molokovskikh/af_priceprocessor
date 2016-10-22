using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class MedSnabKchrParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "TTN")
				.DocumentHeader(h => h.DocumentDate, "TTN_DATE")
				.Line(l => l.Code, "SP_PRD_IDV")
				.Line(l => l.Product, "NAME_POST")
				.Line(l => l.EAN13, "SCAN_CODE")
				.Line(l => l.Quantity, "KOL_VO")
				.Line(l => l.Producer, "PRZV_POST")
				.Line(l => l.Period, "SGODN")
				.Line(l => l.SupplierCost, "PR_MAK_NDS")
				.Line(l => l.SupplierCostWithoutNDS, "PR_MAK")
				.Line(l => l.VitallyImportant, "ZV")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.CertificatesEndDate, "SERT_DATE")
				.Line(l => l.CertificateAuthority, "SERT_AUTH")
				.Line(l => l.SerialNumber, "SERIA")

				.Invoice(i => i.InvoiceNumber, "head_id")
				.Invoice(i => i.RecipientName, "apt_af");
			//.Invoice(i => i.InvoiceDate, "BILLDT");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			var codeIndex = data.Columns.Contains("SP_PRD_IDV");
			var productIndex = data.Columns.Contains("NAME_POST");
			var supplierCostIndex = data.Columns.Contains("PR_MAK_NDS");
			var supplierCostWithoutNdsIndex = data.Columns.Contains("PR_MAK");
			var quantity = data.Columns.Contains("KOL_VO");

			if (codeIndex && productIndex &&
			    supplierCostIndex && supplierCostWithoutNdsIndex &&
			    quantity)
			{
				return true;
			}

			return false;
		}
	}
}

