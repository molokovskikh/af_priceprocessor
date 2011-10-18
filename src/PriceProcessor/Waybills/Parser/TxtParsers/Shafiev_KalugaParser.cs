using System.IO;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class Shafiev_KalugaParser : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
			CodeIndex = 0;
			ProductIndex = 1;
			ProducerIndex = 2;
			CountryIndex = 3;
			QuantityIndex = 4;
			SupplierCostIndex = 5;
			ProducerCostWithoutNdsIndex = 6;				//По описанию тут цена производителя без НДС
			SupplierCostWithoutNdsIndex = 7;
			NdsIndex = 9;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var headerCaption = reader.ReadLine();
				if (!headerCaption.ToLower().Equals("[header]"))
					return false;
				var header = reader.ReadLine().Split(';');
				if (header.Length != 4)
					return false;
				var bodyCaption = reader.ReadLine();
				if (!bodyCaption.ToLower().Equals("[body]"))
					return false;
				var body = reader.ReadLine().Split(';');
				if (GetDecimal(body[21]) != 0)
					return false;
			}
			return true;
		}
	}
}