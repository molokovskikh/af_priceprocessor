using System.Data;
using System.Text;
using Common.Tools;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer.New;
using MySql.Data.MySqlClient;

namespace Inforoom.PriceProcessor.Formalizer
{
	public class NativeDbfPriceParser : InterPriceParser
	{
		public NativeDbfPriceParser(string file, MySqlConnection conn, PriceFormalizationInfo data)
			: base(file, conn, data)
		{
		}

		public override void Open()
		{
			convertedToANSI = true;
			CurrPos = 0;
			//все прайс листы нужно парсить проверяя данные на соответсвие типам
			//кроме одного, т.к. в прайсе полный бред но бодать поставщика смысла нет
			var strict = _info.Price.IsStrict;
			if (priceCode == 2355)
				strict = false;
			dtPrice = Dbf.Load(priceFileName, Encoding.GetEncoding(866), false, strict);
			base.Open();
		}
	}
}