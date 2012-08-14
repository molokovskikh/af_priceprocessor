using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class KatrenVoronezhParser : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
			separator = "|";
			InvoiceNumberIndex = 0;
			InvoiceDateIndex = 1;
			ProviderDocumentIdIndex = 2;
			DocumentDateIndex = 3;
			SellerNameIndex = 4;
			SellerAddressIndex = 5;
			SellerINNIndex = 6;
			SellerKPPIndex = 6;
			ShipperInfoIndex = 7;
			ConsigneeInfoIndex = 8;
			PaymentDocumentInfoIndex = 9;
			BuyerNameIndex = 10;
			BuyerAddressIndex = 11;
			BuyerINNIndex = 12;
			BuyerKPPIndex = 12;
			AmountWithoutNDS0Index = 13;
			AmountWithoutNDS10Index = 14;
			NDSAmount10Index = 15;
			Amount10Index = 16;
			AmountWithoutNDS18Index = 17;
			NDSAmount18Index = 18;
			Amount18Index = 19;
			AmountWithoutNDSIndex = 20;
			InvoiceNDSAmountIndex = 21;
			InvoiceAmountIndex = 22;

			ProductIndex = 1;
			UnitIndex = 2;
			QuantityIndex = 3;
			ProducerIndex = 4;
			SupplierCostIndex = 5;
			SupplierCostWithoutNdsIndex = 6;
			ExciseTaxIndex = 8;
			SupplierPriceMarkupIndex = 9;
			RegistryCostIndex = 10;
			ProducerCostWithoutNdsIndex = 12;
			NdsIndex = 13;
			NdsAmountIndex = 14;
			AmountIndex = 15;
			SerialNumberIndex = 16;
			PeriodIndex = 17;
			CertificatesIndex = 18;
			CountryIndex = 19;
			BillOfEntryNumberIndex = 20;
			CertificatesDateIndex = 21;
			VitallyImportantIndex = 22;
			EAN13Index = 23;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251))) {
				var headerCaption = reader.ReadLine();
				if (!headerCaption.ToLower().Equals("[header]"))
					return false;
				var header = reader.ReadLine().Split('|');
				if (header.Length != 23) return false;
				if (!header[4].ToLower().Contains("катрен")) return false;
				var bodyCaption = reader.ReadLine();
				if (!bodyCaption.ToLower().Equals("[body]")) return false;
				var body = reader.ReadLine().Split('|');
				if (body.Length != 24) return false;
				if (body[0] != "1") return false;
			}
			return true;
		}
	}
}