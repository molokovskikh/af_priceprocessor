using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class BikovCheboksary12649Parser : BaseDbfParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NOM_SHET") &&
				data.Columns.Contains("DATA_SHET") &&
				data.Columns.Contains("CENA_ZAV") &&
				data.Columns.Contains("CENA_OTP") &&
				data.Columns.Contains("NOM_SERT") &&
				data.Columns.Contains("DATA_SERT");
		}

		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.DocumentDate, "DATA_SHET")
				.DocumentHeader(h => h.ProviderDocumentId, "NOM_SHET")
				.Invoice(i => i.InvoiceNumber, "NOM_SHET")
				.Invoice(i => i.InvoiceDate, "DATA_SHET")
				.Invoice(i => i.BuyerName, "APTEKA")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "NAIM")
				.Line(l => l.Producer, "ZAVOD")
				.Line(l => l.ProducerCost, "CENA_ZAV")
				.Line(l => l.SupplierCost, "CENA_OTP")
				.Line(l => l.NdsAmount, "SUMMA_NDS")
				.Line(l => l.Quantity, "KOL")
				.Line(l => l.Period, "SROK")
				.Line(l => l.Certificates, "NOM_SERT")
				.Line(l => l.CertificatesDate, "DATA_SERT")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.Nds, "NDS");
		}

		public override void PostParsing(Document doc)
		{
			foreach (var documentLine in doc.Lines) {
				if (!string.IsNullOrEmpty(documentLine.Period))
					documentLine.Period = DateTime.ParseExact(documentLine.Period, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None).ToShortDateString();
				if (!string.IsNullOrEmpty(documentLine.CertificatesDate))
					documentLine.CertificatesDate = DateTime.ParseExact(documentLine.CertificatesDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None).ToShortDateString();
			}
		}
	}
}