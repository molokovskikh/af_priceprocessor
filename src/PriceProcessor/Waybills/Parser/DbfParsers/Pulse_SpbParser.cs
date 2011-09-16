using System;
using System.Data;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class Pulse_SpbParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file);
			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NDOC")
				.DocumentHeader(d => d.DocumentDate, "DATEDOC")
				.Line(l => l.Code, "SKOD")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Nds, "NALOG1")
				.Line(l => l.Quantity, "KOL")
				.Line(l => l.ProducerCost, "CENAPRNDS")
				.Line(l => l.SupplierCost, "CENA")
				.Line(l => l.SupplierCostWithoutNDS, "CENANONDS")
				.Line(l => l.RegistryCost, "CENAREESTR")
				.Line(l => l.Period, "SROK") //
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.Certificates, "SERTIF")
				//.Line(l => l.Code, "CODP")
				//.Line(l => l.)
				.Line(l => l.VitallyImportant, "GNVLS")
				.ToDocument(document, data);
			return document;
		}

		public static bool CheckFileFormat(DataTable table)
		{
			return table.Columns.Contains("CENAREESTR")
				&& table.Columns.Contains("CENANONDS")
				&& table.Columns.Contains("NALOG1")
				//&& table.Columns.Contains("CODP")
				&& table.Columns.Contains("SROK")
				&& table.Columns.Contains("GNVLS")
				&& table.Columns.Contains("SER");
		}
	}
}
