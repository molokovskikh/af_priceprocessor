using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Parser.TxtParsers;

namespace Inforoom.PriceProcessor.Waybills.Parser.SstParsers
{
	public class FarmImpeksKalugaParser : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
			ProviderDocumentIdIndex = 0;
			DocumentDateIndex = 1;
			InvoiceAmountIndex = 2;

			CodeIndex = 0;
			ProductIndex = 1;
			ProducerIndex = 2;
			CountryIndex = 3;
			QuantityIndex = 4;
			SupplierCostIndex = 5;
			ProducerCostWithoutNdsIndex = 6;
			SupplierCostWithoutNdsIndex = 7;
			BillOfEntryNumberIndex = 11;
			CertificatesIndex = 12;
			PeriodIndex = 15;
			EAN13Index = 16;
			RegistryCostIndex = 18;
			VitallyImportantIndex = 21;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(866))) {
				var headerCaption = reader.ReadLine();
				if (!headerCaption.ToLower().Equals("[header]"))
					return false;
				var header = reader.ReadLine().Split(';');
				if (header.Length != 8)
					return false;
				var bodyCaption = reader.ReadLine();
				if (!bodyCaption.ToLower().Equals("[body]"))
					return false;
				var body = reader.ReadLine().Split(';');
				if (body.Length != 22)
					return false;
				if (GetDecimal(body[7]) == null)
					return false;
				if (GetDecimal(body[5]) == null)
					return false;
			}
			return true;
		}
	}
}