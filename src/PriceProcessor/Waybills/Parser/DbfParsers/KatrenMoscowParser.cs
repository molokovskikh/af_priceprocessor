using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class KatrenMoscowParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "DocNum")
				.DocumentHeader(h => h.DocumentDate, "DocDate")
				.Line(l => l.Code, "ItemId")
				.Line(l => l.Product, "GoodName")
				.Line(l => l.Quantity, "Amount")
				.Line(l => l.SupplierCost, "Price_NDS")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.NdsAmount, "SummNDS")
				.Line(l => l.Amount, "Summ_NDS")
				.Line(l => l.SerialNumber, "Series")
				.Line(l => l.Period, "LifeTime")
				.Line(l => l.Certificates, "Sertif")
				.Line(l => l.Producer, "Vendor")
				.Line(l => l.Country, "Country")
				.Line(l => l.BillOfEntryNumber, "GTD");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("ItemId")
				&& data.Columns.Contains("SummNDS")
				&& data.Columns.Contains("Summ_NDS");
		}
	}
}
