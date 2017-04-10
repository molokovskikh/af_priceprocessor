using System.IO;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Parser.TxtParsers;

namespace Inforoom.PriceProcessor.Waybills.Parser.SstParsers
{
	public class LekFarmParser : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
			ProviderDocumentIdIndex = 0;
			DocumentDateIndex = 1;
			InvoiceAmountIndex = 2;
			BuyerIdIndex = 3;

			CodeIndex = 0;
			ProductIndex = 1;
			ProducerIndex = 2;
			CountryIndex = 3;
			QuantityIndex = 4;
			SupplierCostIndex = 5;
			AmountIndex = 8;
			PeriodIndex = 9;
			BillOfEntryNumberIndex = 11;
			SerialNumberIndex = 12;
			CertificateAuthorityIndex = 14;
			CertificatesDateIndex = 15;
			CertificatesEndDateIndex = 16;
			NdsIndex = 17;
			VitallyImportantIndex = 18;
			RegistryCostIndex = 19;
			ProducerCostWithoutNdsIndex = 20;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251))) {
				var headerCaption = reader.ReadLine();
				if (!headerCaption.ToLower().Equals("[header]"))
					return false;
				var header = reader.ReadLine().Split(';');
				if (header.Length != 9)
					return false;
				var bodyCaption = reader.ReadLine();
				if (!bodyCaption.ToLower().Equals("[body]"))
					return false;
				var body = reader.ReadLine().Split(';');
				if (body.Length != 22)
					return false;
				var s = "";
				var startIndexOfEmptyCells = 4; // индекс ячейки, скоторой и далее все ячейки пустые 
				for (int i = startIndexOfEmptyCells; i < 9; i++)
					s += header[i];
				if (s.Length > 0)
					return false;
			}
			return true;
		}
	}
}