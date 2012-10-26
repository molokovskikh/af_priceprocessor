using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class KatrenOrelParser : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
			InvoiceAmountIndex = 2;
			InvoiceNDSAmountIndex = 4;

			CodeIndex = 0;
			ProductIndex = 1;
			ProducerIndex = 2;
			CountryIndex = 3;
			QuantityIndex = 4;
			SupplierCostIndex = 5;
			SupplierCostWithoutNdsIndex = 7;
			NdsIndex = 9;
			BillOfEntryNumberIndex = 11;
			CertificatesIndex = 12;
			SerialNumberIndex = 13;
			PeriodIndex = 15;
			EAN13Index = 16;
			RegistryCostIndex = 18;
			AmountIndex = 19;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251))) {
				var headerCaption = reader.ReadLine();
				if (!headerCaption.ToLower().Equals("[header]"))
					return false;
				var header = reader.ReadLine().Split(';');
				if ((header.Length != 9) || !header[6].ToLower().Contains("r"))
					return false;
				var bodyCaption = reader.ReadLine();
				if (!bodyCaption.ToLower().Equals("[body]"))
					return false;
				var body = reader.ReadLine().Split(';');
				if(body.Length != 22)
					return false;
			}
			return true;
		}
	}
}
