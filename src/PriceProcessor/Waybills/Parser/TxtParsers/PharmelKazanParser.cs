using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class PharmelKazanParser : BaseIndexingParser
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
			SupplierCostWithoutNdsIndex = 5;
			NdsIndex = 6;
			SupplierCostIndex = 7;
			SerialNumberIndex = 8;
			PeriodIndex = 9;
			CertificatesIndex = 11;
			VitallyImportantIndex = 16;
			ProducerCostWithoutNdsIndex = 17;
			RegistryCostIndex = 18;
			NdsAmountIndex = 20;
			AmountIndex = 21;
		}

		public static bool CheckFileFormat(string file)
		{			
			return CheckByHeaderPart(file, new[] { "Pharmel" });
		}
	}
}
