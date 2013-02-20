using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class SiaKrasnodarParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NDOC")
				.DocumentHeader(d => d.DocumentDate, "DATEDOC")
				.Line(l => l.Code, "CODEPST")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Country, "CNTR")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.SupplierCost, "PRICE2")
				.Line(l => l.ProducerCost, "PRICE1")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.Amount, "SUMPAY")
				.Line(l => l.NdsAmount, "SUMNDS2")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.BillOfEntryNumber, "NUMGTD")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.Period, "GDATE")
				.Line(l => l.Certificates, "SERTIF");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("GDATE")
				&& data.Columns.Contains("SUMNDS2")
				&& data.Columns.Contains("PRICE1")
				&& data.Columns.Contains("PRICE2");
		}
	}
}
