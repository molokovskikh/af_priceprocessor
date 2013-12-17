using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class PulsBrianskParser : BestsellerGroupParser
	{
		public override DbfParser GetParser()
		{
			var parser = base.GetParser();

			return parser
				.Invoice(i => i.RecipientId, "customerCD")
				.Invoice(i => i.RecipientAddress, "customerNM")
				.Line(i => i.OrderId, "orderID");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("DOCNO")
				&& data.Columns.Contains("DOCDAT")
				&& data.Columns.Contains("QUANT")
				&& data.Columns.Contains("orderID")
				&& data.Columns.Contains("PRICEWONDS");
		}
	}
}
