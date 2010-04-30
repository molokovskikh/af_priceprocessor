using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class IzhevskFarmParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NTTN")
				.DocumentHeader(h => h.DocumentDate, "DTTN")
				.Line(l => l.Code, new[] { "KOD" })
				.Line(l => l.Product, new[] { "TOVAR" })
				.Line(l => l.Producer, new[] { "IZGOT" })
				.Line(l => l.Country, new[] { "STRANA" })
				.Line(l => l.ProducerCost, new[] { "CENAIZG" })
				.Line(l => l.SupplierCostWithoutNDS, new[] { "CENABEZNDS" })
				.Line(l => l.Quantity, new[] { "KOLVO" })
				.Line(l => l.Period, new[] { "GODENDO" })
				.Line(l => l.RegistryCost, new[] { "CENAREESTR" })
				.Line(l => l.Certificates, new[] { "SERT" })
				.Line(l => l.SerialNumber, new[] { "SERIA" })
				.Line(l => l.VitallyImportant, new[] { "JNVLS" })
				.Line(l => l.Nds, new[] { "STAVKANDS" })
				.Line(l => l.SetSupplierCostByNds((decimal)l.Nds))
				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			var data = Dbf.Load(file);
			return data.Columns.Contains("NTTN") &&
				   data.Columns.Contains("TOVAR") &&
				   data.Columns.Contains("IZGOT") &&
				   data.Columns.Contains("CENABEZNDS") &&
				   data.Columns.Contains("KOLVO") &&
				   data.Columns.Contains("STAVKANDS");
		}
	}
}
