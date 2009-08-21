﻿using MySql.Data.MySqlClient;
using System.Data;

namespace Inforoom.Formalizer
{
	public class TXTDosDelimiterPriceParser : TXTDelimiterPriceParser
	{
		public TXTDosDelimiterPriceParser(string PriceFileName, MySqlConnection conn, DataTable mydr)
			: base(PriceFileName, conn, mydr)
		{
			FileEncoding = "OEM";
		}

	}
}
