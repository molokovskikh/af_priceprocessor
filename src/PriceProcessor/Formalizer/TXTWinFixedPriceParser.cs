using MySql.Data.MySqlClient;
using System.Data;

namespace Inforoom.Formalizer
{
	public class TXTWinFixedPriceParser : TXTFixedPriceParser
	{
		public TXTWinFixedPriceParser(string PriceFileName, MySqlConnection conn, DataTable mydr)
			: base(PriceFileName, conn, mydr)
		{
			FileEncoding = "ANSI"; 
		}
	}
}
