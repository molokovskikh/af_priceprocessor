using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class AptekaHoldingKazanParser2 : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
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
			CertificatesDateIndex = 14;
			RegistryCostIndex = 22;
			EAN13Index = 26;
			VitallyImportantIndex = 27;
			AmountIndex = 28;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251))) {
				var headerCaption = reader.ReadLine();
				if (!headerCaption.ToLower().Equals("[header]"))
					return false;
				var header = reader.ReadLine().Split(';');
				if (header.Length != 7)
					return false;
				if (!header[3].ToLower().Equals("аптека-холдинг"))
					return false;
				var bodyCaption = reader.ReadLine();
				if (!bodyCaption.ToLower().Equals("[body]"))
					return false;
				var body = reader.ReadLine().Split(';');
				if (body.Length != 29)
					return false;
			}
			return true;
		}
	}
}
