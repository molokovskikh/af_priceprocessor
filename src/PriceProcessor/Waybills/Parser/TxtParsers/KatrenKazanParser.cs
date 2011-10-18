using System.IO;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class KatrenKazanParser : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
			ProviderDocumentIdIndex = 0;
			DocumentDateIndex = 1;

			CodeIndex = 0;
			ProductIndex = 1;
			ProducerIndex = 2;
			CountryIndex = 3;
			QuantityIndex = 4;
			ProducerCostWithoutNdsIndex = 5;
			SupplierCostWithoutNdsIndex = 6;
			NdsIndex = 7;
			NdsAmountIndex = 8;
			SerialNumberIndex = 9;
			PeriodIndex = 10;
			BillOfEntryNumberIndex = 11;
			CertificatesIndex = 12;
			RegistryCostIndex = 16;
			AmountIndex = 17;
			SupplierCostIndex = 18;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var headerCaption = reader.ReadLine();
				if (!headerCaption.ToLower().Equals("[header]"))
					return false;
				var header = reader.ReadLine().Split(';');
				if ((header.Length != 6) || !header[3].ToLower().Contains("зао нпк катрен"))
					return false;
				var bodyCaption = reader.ReadLine();
				if (!bodyCaption.ToLower().Equals("[body]"))
					return false;				
				var body = reader.ReadLine().Split(';');
				if (body.Length != 20)
					return false;
				if (GetDecimal(body[6]) == null)
					return false;
				if (GetDecimal(body[7]) == null)
					return false;
			}
			return true;
		}
	}
}
