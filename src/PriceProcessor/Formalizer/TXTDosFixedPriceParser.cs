using MySql.Data.MySqlClient;
using System.Data;

namespace Inforoom.Formalizer
{
	public class TXTDosFixedPriceParser : TXTFixedPriceParser
	{
		public TXTDosFixedPriceParser(string PriceFileName, MySqlConnection conn, DataTable mydr)
			: base(PriceFileName, conn, mydr)
		{
			FileEncoding = "OEM"; 
		}
	}
}
