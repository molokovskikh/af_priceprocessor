using System.Data;
using System.Text;
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
			//все прайс листы нужно парсить проверяя данные на соответсвие типам
			//кроме одного, т.к. в прайсе полный бред но бодать поставщика смысла нет
			var strict = true;
			if (priceCode == 2355)
				strict = false;
			dtPrice = Dbf.Load(priceFileName, Encoding.GetEncoding(866), false, strict);
			base.Open();
		}
	}
}
