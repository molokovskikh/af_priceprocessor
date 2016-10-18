using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class BellaVostokParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "N_NAKL")
				.DocumentHeader(h => h.DocumentDate, "D_NAKL")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "NAME")
				.Line(l => l.EAN13, "BARCODE")
				.Line(l => l.Quantity, "QUANTITY")
				.Line(l => l.Country, "CONTRY")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.Period, "SROKGDN")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.SupplierCost, "CENANDS")
				.Line(l => l.SupplierCostWithoutNDS, "CENA")
				.Line(l => l.VitallyImportant, "GV")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "SERTDATE")
				.Line(l => l.DateOfManufacture, "MADEDAT")
				.Line(l => l.SerialNumber, "SER")

				//.Invoice(i => i.InvoiceNumber, "BILLNUM")
				.Invoice(i => i.RecipientName, "CODE_CL");
				//.Invoice(i => i.InvoiceDate, "BILLDT");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			var codeIndex = data.Columns.Contains("CODE");
			var productIndex = data.Columns.Contains("NAME");
			var supplierCostIndex = data.Columns.Contains("СENANDS");
			var supplierCostWithoutNdsIndex = data.Columns.Contains("CENA");
			var ndsIndex = data.Columns.Contains("NDS");
			var productionDate = data.Columns.Contains("MADEDAT");



			if (!codeIndex || !productIndex || !productionDate)
				return false;

			if (supplierCostIndex && supplierCostWithoutNdsIndex)
				return true;
			if (supplierCostIndex && ndsIndex)
				return true;
			if (supplierCostWithoutNdsIndex && ndsIndex)
				return true;
			return false;
		}
	}
}
