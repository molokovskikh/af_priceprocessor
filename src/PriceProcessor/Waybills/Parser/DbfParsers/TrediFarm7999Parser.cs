using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class TrediFarm7999Parser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file);
			new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "DATA_NAKL")
				.DocumentHeader(h => h.ProviderDocumentId, "NUM_NAKL")
				.DocumentInvoice(i => i.RecipientAddress, "ADDRESS")
				.DocumentInvoice(i => i.InvoiceNumber, "SF")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.ProducerCostWithoutNDS, "CENAPROIZ")
				.Line(l => l.ProducerCost, "CENAPRNDS")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.SupplierCostWithoutNDS, "CENABNDS")
				.Line(l => l.SupplierCost, "CENASNDS")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.SerialNumber, "SERII")
				.Line(l => l.Period, "EXPDATE")
				.Line(l => l.Nds, "NDSPOSTAV")
				.Line(l => l.NdsAmount, "SUMMANDS")
				.Line(l => l.Amount, "SUMMA")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "DATAEND")
				.Line(l => l.CertificateAuthority, "SERTORG")
				.Line(l => l.BillOfEntryNumber, "N_DECLAR")
				.Line(l => l.VitallyImportant, "PV")
				.ToDocument(document, data);
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NUM_NAKL")
				&& data.Columns.Contains("SF")
				&& data.Columns.Contains("N_DECLAR")
				&& data.Columns.Contains("SERII")
				&& data.Columns.Contains("DATA_NAKL");
		}
	}
}
