using MySql.Data.MySqlClient;
using System.Data;

namespace Inforoom.Formalizer
{
	class TXTWinDelimiterPriceParser : TXTDelimiterPriceParser
	{
		public TXTWinDelimiterPriceParser(string PriceFileName, MySqlConnection conn, DataTable mydr)
			: base(PriceFileName, conn, mydr)
		{
			FileEncoding = "ANSI";
		}
	}
}
