using System;
using System.IO;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class MedlineShustovaParser : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
			SellerINNIndex = 0;
			ProviderDocumentIdIndex = 1;
			DocumentDateIndex = 2;
			InvoiceAmountIndex = 3;
			InvoiceNumberIndex = 6;
			BuyerAddressIndex = 7;
			
			CodeIndex = 0;
			ProductIndex = 2;
			AmountIndex = 5;
			NdsIndex = 6;
			SerialNumberIndex = 7;
			PeriodIndex = 8;
			CertificatesIndex = 10;
			CertificateAuthorityIndex = 11;
			CertificatesEndDateIndex = 12;
			VitallyImportantIndex = 13;
			ProducerCostWithoutNdsIndex = 14;
		}

		public override Document Parse(string file, Document document)
		{
			SetIndexes();
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var headers = reader.ReadLine().Split('|');

				if (document.Invoice == null)
					document.SetInvoice();
				document.ProviderDocumentId = GetString(headers[ProviderDocumentIdIndex]);
				document.DocumentDate = GetDateTime(headers[DocumentDateIndex]);

				document.Invoice.Amount = GetDecimal(headers[InvoiceAmountIndex]);
				document.Invoice.InvoiceNumber = GetString(headers[InvoiceNumberIndex]);
				document.Invoice.BuyerAddress = GetString(headers[BuyerAddressIndex]);
				document.Invoice.SellerINN = GetString(headers[SellerINNIndex]);

				string line;
				while ((line = reader.ReadLine()) != null)
				{
					//пустые строки игнорируются (последняя в примере была пустой)
					if (string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line))
						continue;

					var parts = line.Split('|');
					var docLine = document.NewLine();
					docLine.Code = GetString(parts[CodeIndex]);
					docLine.Product = GetString(parts[ProductIndex]);
					docLine.VitallyImportant = GetBool(parts[VitallyImportantIndex]);
					docLine.SerialNumber = GetString(parts[SerialNumberIndex]);
					docLine.Period = GetString(parts[PeriodIndex]);
					docLine.Certificates = GetString(parts[CertificatesIndex]);
					docLine.CertificatesEndDate = GetDateTime(parts[CertificatesEndDateIndex]);
					docLine.CertificateAuthority = GetString(parts[CertificateAuthorityIndex]);
					docLine.Nds = (uint?) GetInteger(parts[NdsIndex]);
					docLine.ProducerCostWithoutNDS = GetDecimal(parts[ProducerCostWithoutNdsIndex]);
					docLine.Amount = GetDecimal(parts[AmountIndex]);
				}
			}
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var headers = reader.ReadLine().Split('|');
				if (headers.Length != 8)
					return false;

				DateTime dt;
				if (!DateTime.TryParse(headers[2], out dt))
					return false;

				string line = reader.ReadLine();
				var lines = line.Split('|');
				if (lines.Length != 15)
					return false;
				if (!DateTime.TryParse(lines[8], out dt))
					return false;
				return true;
			}
		}
	}
}
