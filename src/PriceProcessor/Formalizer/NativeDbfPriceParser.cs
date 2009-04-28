using System.Data;
using Common.Tools;
using Inforoom.Formalizer;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class NativeDbfPriceParser : InterPriceParser
	{
		public NativeDbfPriceParser(string PriceFileName, MySqlConnection conn, DataTable mydr) 
			: base(PriceFileName, conn, mydr)
		{}

		public override void Open()
		{
			convertedToANSI = true;
			CurrPos = 0;
			dtPrice = Dbf.Load(priceFileName);
			base.Open();
		}
	}
}
