using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class FarmSKDParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NomNakl")
				.DocumentHeader(h => h.DocumentDate, "DateNakl")

				.Line(l => l.Code, "Kod")
				.Line(l => l.Product, "Naim")

				.Line(l => l.Producer, "Proizv")
				.Line(l => l.Country, "Strana")

				.Line(l => l.SupplierCostWithoutNDS, "ZenaBezNDS")
				.Line(l => l.SupplierCost, "ZenaSNDS")

				.Line(l => l.ProducerCostWithoutNDS, "ZenaProizv")
				.Line(l => l.ProducerCost, "ZenaPrSNDS")

				.Line(l => l.RegistryCost, "ZenaReestr")
				.Line(l => l.Nds, "NDS")

				.Line(l => l.Amount, "SumSNDS")
				.Line(l => l.NdsAmount, "NDSSUM")

				.Line(l => l.Quantity, "Kol")
				.Line(l => l.Unit, "EdIzm")

				.Line(l => l.Period, "SrokGodn")
				.Line(l => l.Certificates, "ImSS")
				.Line(l => l.CertificatesDate, "DateImSS")
				.Line(l => l.SerialNumber, "Seria")
				.Line(l => l.BillOfEntryNumber, "TamozhDek")

				.Line(l => l.EAN13, "EAN8_13")
				.Line(l => l.VitallyImportant, "ISLIFE")

				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NomNakl") &&
				   data.Columns.Contains("DateNakl") &&
				   data.Columns.Contains("EdIzm") &&
				   data.Columns.Contains("GodenImSS") &&
				   data.Columns.Contains("ZenaPrSNDS") &&
				   data.Columns.Contains("EAN8_13");
		}
	}
}
