using System.Data;
using Inforoom.Data;
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
			CurrPos = 0;
			dtPrice = DBF.Load(priceFileName);
			base.Open();
		}
	}
}
