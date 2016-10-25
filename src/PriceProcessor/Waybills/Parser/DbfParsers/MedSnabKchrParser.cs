 using System.Data;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class MedSnabKchrParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(1251);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);
			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "TTN")
				.DocumentHeader(h => h.DocumentDate, "TTN_DATE")
				.Line(l => l.Code, "SP_PRD_IDV")
				.Line(l => l.Product, "NAME_POST")
				.Line(l => l.EAN13, "SCAN_CODE")
				.Line(l => l.Quantity, "KOL_VO")
				.Line(l => l.Producer, "PRZV_POST")
				.Line(l => l.Period, "SGODN")
				.Line(l => l.ProducerCost, "PR_MAK_NDS")
				.Line(l => l.ProducerCostWithoutNDS, "PR_MAK")
				.Line(l => l.VitallyImportant, "ZV")
				.Line(l => l.RegistryCost, "PR_REE")
				.Line(l => l.RegistryDate, "DATE_REG")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.CertificatesEndDate, "SERT_DATE")
				.Line(l => l.CertificateAuthority, "SERT_AUTH")
				.Line(l => l.SerialNumber, "SERIA")

				.Invoice(i => i.InvoiceNumber, "head_id")
				.Invoice(i => i.RecipientName, "apt_af")
				.ToDocument(document, data);
			return document;
		}


		public static bool CheckFileFormat(DataTable data)
		{
			var codeIndex = data.Columns.Contains("SP_PRD_IDV");
			var productIndex = data.Columns.Contains("NAME_POST");
			var supplierCostIndex = data.Columns.Contains("PR_MAK_NDS");
			var supplierCostWithoutNdsIndex = data.Columns.Contains("PR_MAK");
			var quantity = data.Columns.Contains("KOL_VO");
			var gtd = data.Columns.Contains("GTD");
			var zv = data.Columns.Contains("ZV");

			if (!codeIndex || !productIndex || !gtd)
				return false;

			if (supplierCostIndex && supplierCostWithoutNdsIndex)
				return true;
			if (supplierCostIndex && quantity)
				return true;
			if (supplierCostWithoutNdsIndex && zv)
				return true;
			return false;
		}
	}
}

