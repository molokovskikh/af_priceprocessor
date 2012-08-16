using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class DedenkoTxtParser : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
			InvoiceAmountIndex = 2;
			ShipperInfoIndex = 3;
			ConsigneeInfoIndex = 4;

			base.SetIndexes();

			ProducerCostWithoutNdsIndex = -1;
			SupplierCostWithoutNdsIndex = 6;
			SupplierCostIndex = 7;
			NdsIndex = 8;
			PeriodIndex = 11;
			SupplierPriceMarkupIndex = -1;
			SerialNumberIndex = -1;
			CertificatesIndex = -1;
			RegistryCostIndex = -1;
			VitallyImportantIndex = -1;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251))) {
				var headerCaption = reader.ReadLine();
				if (!headerCaption.ToLower().Equals("[header]"))
					return false;
				var header = reader.ReadLine().Split(';');
				if ((header.Length != 7))
					return false;
				var bodyCaption = reader.ReadLine();
				if (!bodyCaption.ToLower().Equals("[body]"))
					return false;
				var body = reader.ReadLine().Split(';');
				if (GetDecimal(body[8]) != 0)
					return false;
				if (GetDecimal(body[5]) != null)
					return false;
				if (GetDecimal(body[6]) == null)
					return false;
				if (GetDateTime(body[11]) == null)
					return false;
			}
			return true;
		}
	}
}