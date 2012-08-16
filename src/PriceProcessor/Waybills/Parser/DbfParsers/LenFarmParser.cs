using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class LenFarmParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NOMER")
				.DocumentHeader(h => h.DocumentDate, "DATE")
				.DocumentInvoice(i => i.InvoiceNumber, "NDOCREG")
				.DocumentInvoice(i => i.BuyerName, "KLI")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NM")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.SupplierCostWithoutNDS, "PRICMNDS")
				.Line(l => l.SupplierCost, "PRICWNDS")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.Amount, "SUMMA")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.Period, "SROKGODN")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "SROKSERT")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("PRICMNDS") &&
				data.Columns.Contains("SROKGODN") &&
				data.Columns.Contains("REESTR") &&
				data.Columns.Contains("NOMER") &&
				data.Columns.Contains("NM") &&
				data.Columns.Contains("SERTIF") &&
				!data.Columns.Contains("PRICE_PROI") &&
				data.Columns.Contains("KLI");
		}
	}
}