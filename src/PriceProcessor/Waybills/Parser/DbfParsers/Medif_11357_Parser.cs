﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class Medif_11357_Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "Numdoc")
				.DocumentHeader(d => d.DocumentDate, "Date")
				.Line(l => l.Product, "Name")
				.Line(l => l.Quantity, "Kol")
				.Line(l => l.Certificates, "Sertificat")
				.Line(l => l.Period, "Date_end")
				.Line(l => l.SerialNumber, "Seria")
				.Line(l => l.Country, "Country")
				.Line(l => l.Producer, "Maker")
				.Line(l => l.Nds, "Nds_Tax")
				.Line(l => l.SupplierCostWithoutNDS, "Cena0")
				.Line(l => l.ProducerCostWithoutNDS, "CENAPROIZV")
				.Line(l => l.Code, "NNUM")
				.Line(l => l.Unit, "ED")
				.Line(l => l.NdsAmount, "NDS_SUM")
				.Line(l => l.Amount, "SUMMA")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.VitallyImportant, "TYPET")
				.Line(l => l.RegistryCost, "CGR");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("KONTRAGENT") &&
				data.Columns.Contains("NUMDOC") &&
				data.Columns.Contains("DATE") &&
				data.Columns.Contains("NAME") &&
				data.Columns.Contains("DATE_END") &&
				data.Columns.Contains("SERTIFICAT") &&
				data.Columns.Contains("CODE");
		}
	}
}