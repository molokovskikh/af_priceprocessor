	using System;
	using System.Data;
	using System.Linq;
	using Common.Tools;
	using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
	{
		public class FarmSet_VoronezhParser : IDocumentParser
		{
			public Document Parse(string file, Document document)
			{
				var data = Dbf.Load(file);
				new DbfParser()
					.DocumentHeader(h => h.ProviderDocumentId, "DOCNO")
					.DocumentHeader(h => h.DocumentDate, "DOCDAT")
					.Line(l => l.Code, "CODTOVAR")
					.Line(l => l.Product, "TOVARNAME")
					.Line(l => l.Producer, "PROIZV")
					.Line(l => l.Country, "STRANA")
					.Line(l => l.Quantity, "KOLVO")
					.Line(l => l.Nds, "NDS")
					.Line(l => l.SupplierCostWithoutNDS, "CENAPOST")
					.Line(l => l.SupplierCost, "CENASNDS")
					.Line(l => l.SerialNumber, "SERIA")
					.Line(l => l.Certificates, "SERT")
					.Line(l => l.Period, "SROK")
					.Line(l => l.RegistryCost, "CENAREESTR")
					.Line(l => l.ProducerCostWithoutNDS, "CENAPROIZ")
					.Line(l => l.VitallyImportant, "PV")
					.Line(l => l.EAN13, "SHTRIH")
					.ToDocument(document, data);
				return document;
			}

			//private string 

			public static bool CheckFileFormat(DataTable table)
			{
				return table.Columns.Contains("CODTOVAR")
				       && table.Columns.Contains("CENAPOST")
				       && table.Columns.Contains("CENASNDS")
				       && table.Columns.Contains("SROK")
					   && table.Columns.Contains("PV");
			}
		}
	}
