using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class BelotelovParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.Line(l => l.Code, "SP_PRD_ID")
				.Line(l => l.Product, "NAME_POST")
				.Line(l => l.Quantity, "KOL_TOV")
				.Line(l => l.Period, "SGODN")
				.Line(l => l.Country, "CNTR_POST")
				.Line(l => l.Producer, "PRZV_POST")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.SupplierCost, "PCENA_NDS")
				.Line(l => l.SupplierCostWithoutNDS, "PCENA_BNDS")
				.Line(l => l.Unit, "UNIT")
				.Line(l => l.EAN13, "BAR_CODE")
				.Line(l => l.NdsAmount, "NDS_SUM")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.OrderId, "ORDERID");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("SP_PRD_ID") &&
				data.Columns.Contains("PCENA_BNDS") &&
				data.Columns.Contains("PCENA_NDS") &&
				data.Columns.Contains("ORDERID") &&
				data.Columns.Contains("CNTR_POST") &&
				data.Columns.Contains("NAME_POST") &&
				data.Columns.Contains("BAR_CODE");
		}
	}
}
