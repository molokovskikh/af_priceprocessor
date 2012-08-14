﻿using System.Data;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class KazanFarmDbfParser : IDocumentParser
	{
		public static DataTable Load(string file)
		{
			try {
				return Dbf.Load(file);
			}
			catch (DbfException) {
				return Dbf.Load(file, Encoding.GetEncoding(866), true, false);
			}
		}

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding.GetEncoding(866), true, false);

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NUM_NAKL")
				.DocumentHeader(h => h.DocumentDate, "DATA_NAKL")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.ProducerCostWithoutNDS, "CENAFACT")
				.Line(l => l.SupplierCost, "CENASNDS")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.RegistryCost, "CENAREG")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.SerialNumber, "SERII")
				.Line(l => l.VitallyImportant, "JNVLS")
				.Line(l => l.NdsAmount, "SUMMANDS")
				.Line(l => l.Amount, "SUMMA")
				.Line(l => l.Period, "DATAEND")
				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NUM_NAKL") &&
				data.Columns.Contains("DATA_NAKL") &&
				data.Columns.Contains("KOD") &&
				data.Columns.Contains("NAME") &&
				data.Columns.Contains("PROIZV") &&
				data.Columns.Contains("COUNTRY") &&
				data.Columns.Contains("CENAFACT") &&
				data.Columns.Contains("CENASNDS") &&
				data.Columns.Contains("KOLVO") &&
				data.Columns.Contains("CENAREG") &&
				data.Columns.Contains("SERTIF") &&
				data.Columns.Contains("SERII") &&
				data.Columns.Contains("JNVLS") &&
				data.Columns.Contains("SUMMANDS");
		}
	}
}