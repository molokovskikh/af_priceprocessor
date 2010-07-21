using System.IO;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class RostaMskParser : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
			CodeIndex = 0;
			ProductIndex = 1;
			ProducerIndex = 2;
			CountryIndex = 3;
			QuantityIndex = 4;
			ProducerCostIndex = 6;
			SupplierCostIndex = 5;
			SupplierCostWithoutNdsIndex = 7;
			NdsIndex = 24;
			SupplierPriceMarkupIndex = -1;
			SerialNumberIndex = 13;
			PeriodIndex = 15;
			CertificatesIndex = 12;
			RegistryCostIndex = -1;
			VitallyImportantIndex = -1;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var headerCaption = reader.ReadLine();
				if (!headerCaption.ToLower().Equals("[header]"))
					return false;
				var header = reader.ReadLine().Split(';');
				if (header.Length != 12)
					return false;
				if (!header[3].ToLower().Equals("поставка") || 
					!header[6].ToLower().Equals("рубль") || 
					!((header[10].ToLower().Equals("зао роста")) || (header[10].ToLower().Equals(""))))
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
