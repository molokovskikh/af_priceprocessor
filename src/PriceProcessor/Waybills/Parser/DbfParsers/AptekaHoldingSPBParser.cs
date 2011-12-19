using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class AptekaHoldingSPBParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NDOC")
				.DocumentHeader(h => h.DocumentDate, "DATEDOC")

				.DocumentInvoice(i => i.Amount, "SUMPAY")

				.Line(l => l.Code, "CODEPST")
				.Line(l => l.Product, "NAME")

				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Country, "CNTR")

				.Line(l => l.ProducerCost, "PRICEMAN")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE2N")

				.Line(l => l.Amount, "SUMS0")
				.Line(l => l.NdsAmount, "SUMSNDS")

				.Line(l => l.Quantity, "QNT")

				.Line(l => l.Period, "GDATE")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "SERTDATE")

				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.BillOfEntryNumber, "NUMGTD")

				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.VitallyImportant, "GNVLS")

				.Line(l => l.Nds, "NDS")
				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("CODEPST") &&
				   data.Columns.Contains("PRICEMAN") &&
				   data.Columns.Contains("PRICE2N") &&
				   data.Columns.Contains("GDATE") &&
				   data.Columns.Contains("SERTIF") &&
				   data.Columns.Contains("NUMGTD") &&
				   data.Columns.Contains("SUMPAY");
		}
	}
}
