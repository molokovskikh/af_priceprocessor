using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class SiaInternationalParser : BaseIndexingParser
	{
		protected override void SetIndexes()
		{
			SupplierNameIndex = 0;
			InvoiceNumberIndex = 1;
			DocumentDateIndex = 2;

			ProductIndex = 0;
			CountryIndex = 1;
			ProducerIndex = 2;
			CertificatesIndex = 3;
			CertificatesDateIndex = 4;
			CertificateAuthorityIndex = 5;
			NdsIndex = 6;
			SerialNumberIndex = 7;
			PeriodIndex =8;
			ProducerCostIndex = 9;
			RegistryCostIndex= 10;
			SupplierCostIndex = 11;
			QuantityIndex = 12;
		}

		public override Document Parse(string file, Document document)
		{
			SetIndexes();
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251))) {
				var headers = SplitLine(reader.ReadLine());

				//document.SetInvoice().SellerName =
					GetString(headers[SupplierNameIndex]); // нет поля "Наименование поставщика"
				document.ProviderDocumentId = GetString(headers[InvoiceNumberIndex]);
				document.DocumentDate = GetDateTime(headers[DocumentDateIndex]);

				string line;
				while ((line = reader.ReadLine()) != null) {
					var parts = SplitLine(line);
					var docLine = document.NewLine();
					docLine.Product = GetString(parts[ProductIndex]);
					docLine.Country = GetString(parts[CountryIndex]);
					docLine.Producer = GetString(parts[ProducerIndex]);

					docLine.Certificates = GetString(parts[CertificatesIndex]);

					docLine.CertificatesDate = GetString(parts[CertificatesDateIndex]);
					docLine.CertificateAuthority = GetString(parts[CertificateAuthorityIndex]);
					docLine.Nds = (uint?)GetInteger(parts[NdsIndex]);
					docLine.SerialNumber = GetString(parts[SerialNumberIndex]);
					docLine.CertificatesEndDate = GetDateTime(parts[PeriodIndex]);//

					docLine.ProducerCost = GetDecimal(parts[ProducerCostIndex]);
					docLine.RegistryCost = GetDecimal(parts[RegistryCostIndex]);
					docLine.SupplierCost = GetDecimal(parts[SupplierCostIndex]);
					docLine.Quantity = (uint?)GetInteger(parts[QuantityIndex]);

				}
			}
			return document;
		}

		private static string[] SplitLine(string str, string separator = "!!")
		{
			return str.Split(new[] {separator}, StringSplitOptions.None);
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251))) {
				var headers = SplitLine(reader.ReadLine());
				if (headers.Length != 3)
					return false;

				DateTime dt;
				if (!DateTime.TryParse(headers[2], out dt))
					return false;
				string line;
				while ((line = reader.ReadLine()) != null)
					if (SplitLine(line).Length < 13)
						return false;
				return true;
			}
		}
	}
}