using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class PandaKazan19267Parser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(1251);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NOMER")
				.DocumentHeader(h => h.DocumentDate, "DATA")
				.Invoice(i => i.BuyerName, "KLI")
				.Invoice(i => i.BuyerId, "REESTR")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NM")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.SupplierCostWithoutNDS, "PRICMNDS")
				.Line(l => l.SupplierCost, "PRICWNDS")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.Amount, "SUMMA")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.Period, "SROKGODN")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "SERTDATA")
				.Line(l => l.CertificatesEndDate, "SROKSERT")
				.Line(l => l.CertificateAuthority, "SERTKEM")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.ProducerCost, "PRICE_PR")
				.Line(l => l.NdsAmount, "REG_NOM")
				.Line(l => l.RegistryCost, "NAC")
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
				!data.Columns.Contains("DATE") &&
				data.Columns.Contains("DATA") &&
				data.Columns.Contains("KLI") &&
				data.Columns.Contains("REG_NOM");
		}
	}
}