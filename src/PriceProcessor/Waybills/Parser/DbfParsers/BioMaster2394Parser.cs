using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class BioMaster2394Parser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NDOC")
				.DocumentHeader(h => h.DocumentDate, "DATEDOC")
				.Invoice(i => i.InvoiceNumber, "BILLNUM")
				.Invoice(i => i.InvoiceDate, "BILLDT")
				.Invoice(i => i.Amount, "SUMPAY")
				.Line(l => l.Code, "CODEPST")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Country, "CNTR")
				.Line(l => l.ProducerCostWithoutNDS, "PRICEMAN")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE2N")
				.Line(l => l.SupplierCost, "PRICENDS")
				.Line(l => l.RegistryCost, "REGPRC")
				.Line(l => l.Amount, "SUMS0")
				.Line(l => l.NdsAmount, "SUMSNDS")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.Period, "GDATE")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "SERTDATE")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.BillOfEntryNumber, "NUMGTD")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.VitallyImportant, "GNVLS")
				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("PRICEMAN") &&
				data.Columns.Contains("PRICE2N") &&
				data.Columns.Contains("REGPRC") &&
				data.Columns.Contains("CNTRMADE") &&
				data.Columns.Contains("ONTPACK") &&
				data.Columns.Contains("SERTDATE");
		}
	}
}