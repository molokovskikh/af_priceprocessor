using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class Impakt12743Parser : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
			InvoiceAmountIndex = 2;
			base.SetIndexes();

			QuantityIndex = 3;
			UnitIndex = 4;
			SupplierCostIndex = 5;
			AmountIndex = 6;
			NdsIndex = 8;
			NdsAmountIndex = 9;


			ProducerCostWithoutNdsIndex = -1;
			SupplierPriceMarkupIndex = -1;
			SerialNumberIndex = -1;
			PeriodIndex = -1;
			CertificatesIndex = -1;
			RegistryCostIndex = -1;
			VitallyImportantIndex = -1;
			SupplierCostWithoutNdsIndex = -1;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251))) {
				var headerCaption = reader.ReadLine();
				if (!headerCaption.ToLower().Equals("[header]"))
					return false;
				var header = reader.ReadLine().Split(';');
				if ((header.Length != 3))
					return false;
				var bodyCaption = reader.ReadLine();
				if (!bodyCaption.ToLower().Equals("[body]"))
					return false;
				var body = reader.ReadLine().Split(';');
				if ((body.Length != 10))
					return false;
			}
			return true;
		}
	}
}
