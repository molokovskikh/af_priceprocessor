using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class Medkom_Mp_Spb : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NDOC")
				.DocumentHeader(h => h.DocumentDate, "DATEDOC")

				.DocumentInvoice(i=> i.InvoiceNumber, "BILLNUM")
				.DocumentInvoice(i=> i.InvoiceDate, "BILLDT")
				.DocumentInvoice(i=> i.ConsigneeInfo, "PODR")

				.Line(l => l.Code, "CODEPST")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Country, "CNTR")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.ProducerCostWithoutNDS, "PRICEMAN")

				.Line(l => l.Amount, "SUMSNDS")
				.Line(l => l.NdsAmount, "SUMNDS")

				.Line(l => l.Quantity, "QNT")

				.Line(l => l.Period, "GDATE")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.BillOfEntryNumber, "GTD")

				.Line(l => l.Nds, "NDS")
				.Line(l => l.VitallyImportant, "ISLIFE")
				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("BILLDT") &&
				   data.Columns.Contains("BILLNUM") &&
				   data.Columns.Contains("PODR") &&
				   data.Columns.Contains("CODEPST") &&
				   data.Columns.Contains("PRICEMAN") &&
				   data.Columns.Contains("ISLIFE");
		}
	}
}
