using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class SiaAstrahanParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file);

			if (data.Columns.Contains("GNVLS")) {
				foreach (DataRow dataRow in data.Rows) {
					if (dataRow["GNVLS"] is string || dataRow["GNVLS"] is DBNull) {
						if (dataRow["GNVLS"].ToString() == "ЖНВЛС") {
							dataRow["GNVLS"] = 1;
						}
						else {
							dataRow["GNVLS"] = 0;
						}
					}
				}
			}

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "DCODE")
				.DocumentHeader(h => h.DocumentDate, "DATE_DOC")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "PRODUCT")
				.Line(l => l.Producer, "PRODUCER2", "PRODUCER", "PROIZ")
				.Line(l => l.Country, "COUNTRY", "STRANA")
				.Line(l => l.ProducerCostWithoutNDS, "PRO_NNDS")
				.Line(l => l.ProducerCost, "PRO")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE_BASE")
				.Line(l => l.SupplierCost, "PRICE_OPL")
				.Line(l => l.RegistryCost, "PRICE_REES")
				.Line(l => l.SupplierPriceMarkup, "NC_OPT_PR")
				.Line(l => l.Amount, "SUM_OPL")
				.Line(l => l.NdsAmount, "NDS_SUM")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.Period, "SROK_S", "GOD")
				.Line(l => l.Certificates, "SERT_N")
				.Line(l => l.CertificatesDate, "DATE_SERT")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.EAN13, "EAN13", "BARCOD", "BarCode")
				.Line(l => l.Nds, "NDS_PR")
				.Line(l => l.VitallyImportant, "ZNVLS", "VitImport", "PV", "GNVLS", "ISZHVP")
				.Line(l => l.CodeCr, "VCODE", "PROIZCODE")
				.Line(l => l.OrderId, "idorder")
				.Line(l => l.RegistryDate, "DATE_REES")
				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NDS_PR") &&
				data.Columns.Contains("PRICE_REES") &&
				data.Columns.Contains("PRICE_BASE") &&
				data.Columns.Contains("PRO_NNDS") &&
				data.Columns.Contains("SUM_OPL")
				&& data.Columns.Contains("PRODUCT")
				&& (data.Columns.Contains("PRODUCER") || data.Columns.Contains("PRODUCER2") || data.Columns.Contains("PROIZ"));
		}
	}
}