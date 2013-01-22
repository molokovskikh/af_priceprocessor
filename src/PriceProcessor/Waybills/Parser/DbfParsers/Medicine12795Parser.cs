using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	/*public class Medicine12795Parser : PulsFKParser
	{
		public override DbfParser GetParser()
		{
			var parcer = base.GetParser();

			parcer = parcer
				.DocumentInvoice(i => i.NDSAmount10, "SUMNDS10")
				.DocumentInvoice(i => i.NDSAmount18, "SUMNDS20")
				.DocumentInvoice(i => i.AmountWithoutNDS10, "SUM10")
				.DocumentInvoice(i => i.AmountWithoutNDS18, "SUM20")
				.DocumentInvoice(i => i.AmountWithoutNDS0, "SUM0")
				.DocumentInvoice(i => i.RecipientId, "PODRCD");

			return parcer;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("CNTR")
				&& data.Columns.Contains("SERTIF")
				&& data.Columns.Contains("GDATE")
				&& data.Columns.Contains("PRICE2")
				&& data.Columns.Contains("NUMZ")
				&& data.Columns.Contains("PODRCD");
		}
	}*/
}
