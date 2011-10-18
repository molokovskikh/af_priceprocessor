using System.IO;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class DetstvoOmskParser : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
			CodeIndex = 0;
			ProductIndex = 1;
			ProducerIndex = 3;
			CountryIndex = 2;
			QuantityIndex = 4;
			ProducerCostWithoutNdsIndex = 5;
			SupplierCostIndex = 8;
			NdsIndex = -1;
			SupplierPriceMarkupIndex = -1;
			SerialNumberIndex = 13;
			PeriodIndex = 15;
			CertificatesIndex = 12;
			RegistryCostIndex = -1;
			VitallyImportantIndex = 21;
			SupplierCostWithoutNdsIndex = 7;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var headerCaption = reader.ReadLine();
				if (!headerCaption.ToLower().Equals("[header]"))
					return false;
				var header = reader.ReadLine().Split(';');
				if ((header.Length != 9) || !header[3].ToLower().Equals("поставка") || !header[6].ToLower().Equals("рубль"))
					return false;
				var bodyCaption = reader.ReadLine();
				if (!bodyCaption.ToLower().Equals("[body]"))
					return false;
				var body = reader.ReadLine().Split(';');
				if (GetDecimal(body[6]) == null)
					return false;
			}
			return true;
		}
	}
}
