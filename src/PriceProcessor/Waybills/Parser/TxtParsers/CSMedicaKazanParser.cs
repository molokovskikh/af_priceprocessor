using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class CSMedicaKazanParser : BaseIndexingParser
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
			SupplierCostWithoutNdsIndex = 6;
			NdsIndex = 7;
			NdsAmountIndex = 8;
			CertificatesIndex = 12;
			CertificateAuthorityIndex = 13;
			CertificatesDateIndex = 14;
		}

		public static bool CheckFileFormat(string file)
		{
			return CheckByHeaderPart(file, new[] { "ООО \"СиЭс Медика Казань\"" });
		}
	}
}