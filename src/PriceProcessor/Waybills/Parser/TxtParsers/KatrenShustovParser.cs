using System;
using System.IO;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class KatrenShustovParser : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
			SupplierNameIndex = 0;
			InvoiceNumberIndex = 1;
			InvoiceDateIndex = 2;
			PaymentDocumentInfoIndex = 3;
			InvoiceAmountIndex = 4;

			CodeIndex = 0;
			ProductIndex = 1;
			ProducerIndex = 2;
			CountryIndex = 3;
			VitallyImportantIndex = 4;
			SerialNumberIndex = 5;
			PeriodIndex = 6;
			CertificatesIndex = 7;
			CertificatesEndDateIndex = 8;
			CertificateAuthorityIndex = 9;
			NdsIndex = 10;
			QuantityIndex = 11;
			SupplierCostIndex = 12;
			RegistryCostIndex = 13;
			RegistryDateIndex = 14;
			EAN13Index = 15;
			ProducerCostIndex = 16;
			NDSAmount18Index = 17;
		}

		public override Document Parse(string file, Document document)
		{
			SetIndexes();
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251))) {
				var headers = reader.ReadLine().Split('|');

				if (document.Invoice == null)
					document.SetInvoice();
				// нет поля "Наименование поставщика"
				document.Invoice.InvoiceNumber = GetString(headers[InvoiceNumberIndex]);
				document.Invoice.InvoiceDate = GetDateTime(headers[InvoiceDateIndex]);
				document.Invoice.DateOfPaymentDelay = GetString(headers[PaymentDocumentInfoIndex]);

				string line;
				while ((line = reader.ReadLine()) != null) {
					//пустые строки игнорируются (последняя в примере была пустой)
					if (string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line))
						continue;

					var parts = line.Split('|');
					var docLine = document.NewLine();
					docLine.Code = GetString(parts[CodeIndex]);
					docLine.Product = GetString(parts[ProductIndex]);
					docLine.Producer = GetString(parts[ProducerIndex]);
					docLine.Country = GetString(parts[CountryIndex]);
					docLine.VitallyImportant = GetBool(parts[VitallyImportantIndex]);
					docLine.SerialNumber = GetString(parts[SerialNumberIndex]);
					docLine.Period = GetString(parts[PeriodIndex]);
					docLine.Certificates = GetString(parts[CertificatesIndex]);
					docLine.CertificatesEndDate = GetDateTime(parts[CertificatesEndDateIndex]);
					docLine.CertificateAuthority = GetString(parts[CertificateAuthorityIndex]);
					docLine.Nds = (uint?) GetInteger(parts[NdsIndex]);
					docLine.Quantity = (uint?) GetInteger(parts[QuantityIndex]);
					docLine.SupplierCost = GetDecimal(parts[SupplierCostIndex]);
					docLine.RegistryCost = GetDecimal(parts[RegistryCostIndex]);
					docLine.RegistryDate = GetDateTime(parts[RegistryDateIndex]);
					ulong barcode = 0;
					if (ulong.TryParse(GetString(parts[EAN13Index]), out barcode)) {
						docLine.EAN13 = barcode;
					}
					docLine.ProducerCostWithoutNDS = GetDecimal(parts[ProducerCostIndex]);
					docLine.NdsAmount = GetDecimal(parts[NDSAmount18Index]);
				}
			}
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251))) {
				var headers = reader.ReadLine().Split('|');
				if (headers.Length != 5)
					return false;

				DateTime dt;
				decimal amount;
				if (!DateTime.TryParse(headers[2], out dt))
					return false;
				if (!DateTime.TryParse(headers[3], out dt))
					return false;

				string line;
				var result = false;
				while ((line = reader.ReadLine()) != null) {
					//если первая строка содержания не пустая
					if (result == false && !string.IsNullOrEmpty(line) || !string.IsNullOrWhiteSpace(line))
						result = true;
					//остальные пустые строки игнорируются (последняя в примере была пустой)
					if (result && string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line))
						continue;

					var array = line.Split('|');
					if (array.Length != 19 || array[18] != "")
						result = false;
				}
				return result;
			}
		}
	}
}