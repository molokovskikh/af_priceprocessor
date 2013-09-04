using System;
using System.Data;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class AllinceHealthcareParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file);
			document.ProviderDocumentId = data.Rows[0]["SERIA"].ToString();
			document.DocumentDate = Convert.ToDateTime(data.Rows[0]["SROK_GOD"]);
			data.Rows.Remove(data.Rows[0]);

			var parcer = new DbfParser()
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "TOVAR")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.Country, "STRANA")
				.Line(l => l.Period, "SROK_GOD")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.ProducerCostWithoutNDS, "CENA_PRB")
				.Line(l => l.SupplierCostWithoutNDS, "CENA_POB")
				.Line(l => l.Nds, "ST_NDS")
				.Line(l => l.RegistryCost, "CENA_GRU")
				.Line(l => l.Certificates, "SER_NOM")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.VitallyImportant, "VID")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.Amount, "SUM_SNDS")
				.Line(l => l.NdsAmount, "SUM_NDS")
				.Line(l => l.CertificatesDate, "SER_DATE");

			if (data.Columns.Contains("VID")) {
				data.Rows.Cast<DataRow>().Each(r => {
					if (r["VID"].ToString() == "ЖВЛС")
						r["VID"] = "True";
					else
						r["VID"] = "False";
				});
				parcer = parcer.Line(l => l.VitallyImportant, "VID");
			}

			if (data.Columns.Contains("SER_DATE"))
				parcer = parcer.Line(l => l.CertificatesDate, "SER_DATE");

			parcer.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable table)
		{
			return table.Columns.Contains("CENA_GRU")
				&& table.Columns.Contains("CENA_POB");
		}
	}
}