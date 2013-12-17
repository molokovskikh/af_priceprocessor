using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class MedtehKomplektParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NOMERDOK")
				.DocumentHeader(h => h.DocumentDate, "DATADOK")
				.Invoice(i => i.BuyerName, "KLIENT")
				.Invoice(i => i.Amount, "SUMMDOK")
				.Line(l => l.Code, "KODTOV")
				.Line(l => l.Product, "TOV")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.ProducerCostWithoutNDS, "CENAPROIZV")
				.Line(l => l.ProducerCost, "CENAPRNDS")
				.Line(l => l.SupplierCostWithoutNDS, "CENABZNDS")
				.Line(l => l.SupplierCost, "CENASNDS")
				.Line(l => l.RegistryCost, "CENAREESTR")
				.Line(l => l.Amount, "SUMM")
				.Line(l => l.NdsAmount, "SUMMNDS")
				.Line(l => l.Quantity, "KOL")
				.Line(l => l.Period, "USERBYDATE")
				.Line(l => l.Certificates, "SERT_NUM")
				.Line(l => l.CertificatesDate, "SERT_IDATE")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.EAN13, "BARCODE")
				.Line(l => l.Nds, "STNDS")
				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("BARCODE") &&
				data.Columns.Contains("CENAPROIZV") &&
				data.Columns.Contains("SERT_IDATE") &&
				data.Columns.Contains("USERBYDATE") &&
				data.Columns.Contains("KLIENT") &&
				data.Columns.Contains("SUMMDOK");
		}
	}
}