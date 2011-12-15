using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class VitaLineKazanParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "IDDOC")

				.Line(l => l.Code, "ID_MP")
				.Line(l => l.Product, "NAMETOW")

				.Line(l => l.ProducerCostWithoutNDS, "SUMIZGOTWON")
				.Line(l => l.ProducerCost, "SUMIZGOT")
				.Line(l => l.SupplierCostWithoutNDS, "SUMPRIHWON")
				.Line(l => l.SupplierCost, "SUMPRIH")
				.Line(l => l.Producer, "IZGOT")
				.Line(l => l.Country, "STRANA")

				.Line(l => l.Nds, "NDS")
				.Line(l => l.RegistryCost, "PRICE_REG")

				.Line(l => l.Amount, "PRICESWN")
				.Line(l => l.NdsAmount, "SNDS")

				.Line(l => l.Unit, "ED")
				.Line(l => l.EAN13, "EAN13")

				.Line(l => l.Quantity, "AMOUNTOW")

				.Line(l => l.Period, "SROK")
				.Line(l => l.Certificates, "NUMBSERT")
				.Line(l => l.CertificatesDate, "DATASERT")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.BillOfEntryNumber, "NUMBGTD")

				.Line(l => l.VitallyImportant, "GNVLS")
				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("SUMIZGOTWON") &&
				   data.Columns.Contains("ID_MP") &&
				   data.Columns.Contains("SUMPRIH") &&
				   data.Columns.Contains("SUMPRIHWON") &&
				   data.Columns.Contains("PRICESWN") &&
				   data.Columns.Contains("PRICE_REG");
		}
	}
}
