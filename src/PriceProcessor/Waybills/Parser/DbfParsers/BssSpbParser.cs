﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class BssSpbParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "NAKLDATA")
				.DocumentHeader(d => d.ProviderDocumentId, "DOK_NAM")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Country, "STRANA")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE_1")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.Quantity, "KOL")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE_M2")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.Period, "GOD_TO")
				.Line(l => l.Certificates, "SERT_VS")
				.Line(l => l.CertificateAuthority, "SERT_FIRM")
				.Line(l => l.CertificatesDate, "SERT_DATE")
				.Line(l => l.RegistryCost, "PRICE_GR")
				.Line(l => l.VitallyImportant, "GVLS")
				.Line(l => l.CertificatesEndDate, "GOD_SERT")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.EAN13, "EAN13");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NAKLDATA")
				&& data.Columns.Contains("DOK_NAM")
				&& data.Columns.Contains("PRICE_M2")
				&& data.Columns.Contains("GOD_TO")
				&& data.Columns.Contains("KOL")
<<<<<<< HEAD
				&& data.Columns.Contains("EAN13"); // добавлено условие т.к. появился парсер BssSpbWithEan13Parser
=======
				&& !data.Columns.Contains("EAN13"); // добавлено условие т.к. появился парсер BssSpbWithEan13Parser
>>>>>>> ce852ede0b7b4d16bf6771467247072cc9ff1cee
		}
	}
}