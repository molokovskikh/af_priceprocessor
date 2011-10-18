using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class KatrenVrnParser : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
			CodeIndex = 0;
			ProductIndex = 1;
			ProducerIndex = 2;
			CountryIndex = 3;
			QuantityIndex = 4;
			ProducerCostWithoutNdsIndex = 5;
			SupplierCostIndex = 6;
			NdsIndex = 7;
			SupplierPriceMarkupIndex = 8;
			SerialNumberIndex = 10;
			PeriodIndex = 11;
			CertificatesIndex = 13;
			RegistryCostIndex = 17;
			VitallyImportantIndex = 19;
			SupplierCostWithoutNdsIndex = -1;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var headerCaption = reader.ReadLine();
				if (!headerCaption.ToLower().Equals("[header]"))
					return false;
				var header = reader.ReadLine().Split(';');
				if ((header.Length != 7) || !header[3].ToLower().Contains("зао нпк катрен"))
					return false;
				var bodyCaption = reader.ReadLine();
				if (!bodyCaption.ToLower().Equals("[body]"))
					return false;
				var body = reader.ReadLine().Split(';');
				if (GetDecimal(body[6]) == null)
					return false;
				if (GetDateTime(body[11]) == null)
					return false;
				//Так как в столбце с датами могут попадаться символы ".." ,
				//то проверка по этому столбцу не всегда проходит корректно, хотя парсер подходит.
				//if (GetDateTime(body[16]) == null)
					//return false;
			}
			return true;
		}
	}
}
