﻿using System.Data;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class SmileParser: BaseDbfParser
	{			
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "DOCNUM")
				.DocumentHeader(d => d.DocumentDate, "DOCDATE")
				.Line(l => l.Product, "WARESNAME")
				.Line(l => l.Code, "WARESCODE")
				.Line(l => l.Producer, "PRODNAME")
				.Line(l => l.Country, "COUNTRYNAM")
				.Line(l => l.Quantity, "AMOUNT")
				.Line(l => l.SupplierCostWithoutNDS, "PRICEOPT")
				.Line(l => l.ProducerCost, "PRICEPROD")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.Certificates, "CERTNUM")
				.Line(l => l.Period, "WARESVALID");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("WARESNAME")
			       && data.Columns.Contains("WARESCODE")
			       && data.Columns.Contains("PRODNAME")
			       && data.Columns.Contains("WARESVALID");
		}		
	}
}
