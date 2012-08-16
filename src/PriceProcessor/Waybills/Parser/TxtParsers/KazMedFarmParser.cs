using System.IO;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class KazMedFarmParser : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
			ProviderDocumentIdIndex = 0;
			DocumentDateIndex = 1;
			CodeIndex = 0;
			ProductIndex = 1;
			QuantityIndex = 2;
			SupplierCostIndex = 3;
			NdsIndex = 4;
			NdsAmountIndex = 5;
			AmountIndex = 6;
			CertificatesIndex = 7;
			SerialNumberIndex = 8;
			PeriodIndex = 9;
			BillOfEntryNumberIndex = 10;
			EAN13Index = 11;
			ProducerIndex = 12;
			CountryIndex = 14;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251))) {
				var headerCaption = reader.ReadLine();
				if (!headerCaption.ToLower().Equals("[header]"))
					return false;
				var header = reader.ReadLine().Split(';');
				if (header.Length != 6)
					return false;
				if (GetDateTime(header[1]) == null)
					return false;
				var bodyCaption = reader.ReadLine();
				if (!bodyCaption.ToLower().Equals("[body]"))
					return false;
				var body = reader.ReadLine().Split(';');
				if (body.Length != 15)
					return false;
				if (GetDecimal(body[3]) == null)
					return false;
				if (GetDecimal(body[6]) == null)
					return false;
				if (GetDateTime(body[9]) == null)
					return false;
			}
			return true;
		}
	}
}