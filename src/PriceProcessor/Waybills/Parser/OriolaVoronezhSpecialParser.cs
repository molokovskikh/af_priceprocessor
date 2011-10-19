using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class OriolaVoronezhSpecialParser : IDocumentParser
	{
		public static DataTable Load(string file)
		{
			try
			{
				return Dbf.Load(file);
			}
			catch (DbfException)
			{
				return Dbf.Load(file, Encoding.GetEncoding(866), true, false);
			}
		}

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding.GetEncoding(866), true, false);
			new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "DOCNO")
				.DocumentHeader(d => d.DocumentDate, "DOCDAT")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "TOVAR")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.Quantity, "KOL")
				.Line(l => l.SupplierCostWithoutNDS, "TZENA")
				.Line(l => l.NdsAmount, "NDS")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "SERTOT")
				.Line(l => l.Period, "GODEN")
				.Line(l => l.Country, "STRANA")
				.Line(l => l.RegistryCost, "REG")
				.Line(l => l.ProducerCostWithoutNDS, "ZAVOD")
				.Line(l => l.SupplierPriceMarkup, "TORGNADB")
				.Line(l => l.Producer, "PROIZV")								
				.Line(l => l.SupplierCost, "TZENANDS")
				.Line(l => l.Amount, "SUMMANDS")
				.Line(l => l.Nds, "NDSSTAVK")
				.Line(l => l.VitallyImportant, "PV")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.OrderId, "NZAKAZ")
				.ToDocument(document, data);
			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("DOCNO") &&
				   data.Columns.Contains("TOVAR") &&
				   data.Columns.Contains("CODE") &&
				   data.Columns.Contains("PROIZV") &&
				   data.Columns.Contains("TZENANDS") &&
				   data.Columns.Contains("GODEN") &&
				   data.Columns.Contains("KOL") &&
				   data.Columns.Contains("DOCDAT");
		}
	}
}
