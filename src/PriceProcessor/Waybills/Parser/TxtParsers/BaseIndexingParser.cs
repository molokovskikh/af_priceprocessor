using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class HeaderBodyParser : IDisposable
	{
		public enum Part
		{
			None,
			Header,
			Body
		}

		private bool _disposeReader;
		private string _commentMark;
		private StreamReader _reader;
		private Part part;

		public HeaderBodyParser(StreamReader reader, string commentMark)
		{
			_reader = reader;
			_commentMark = commentMark;
		}

		public HeaderBodyParser(string file, string commentMark)
		{
			_reader = new StreamReader(file, Encoding.GetEncoding(1251));
			_disposeReader = true;
			_commentMark = commentMark;
		}

		public IEnumerable<string> Lines()
		{
			string line;
			while ((line = _reader.ReadLine()) != null) {
				yield return line;
			}
		}

		public IEnumerable<string> Header()
		{
			foreach (var line in Lines().Where(l => !String.IsNullOrWhiteSpace(l)).Where(l => String.IsNullOrEmpty(_commentMark) || !l.StartsWith(_commentMark))) {
				if (part == Part.None && (line.ToLower() == "[header]" || line.ToLower() == "[заголовок]")) {
					part = Part.Header;
					continue;
				}

				if (part == Part.Header)
					yield return line;
			}
		}

		public IEnumerable<string> Body()
		{
			foreach (var line in Lines().Where(l => !String.IsNullOrWhiteSpace(l)).Where(l => String.IsNullOrEmpty(_commentMark) || !l.StartsWith(_commentMark))) {
				if (part == Part.Header && (line.ToLower() == "[body]" || line.ToLower() == "[таблица]")) {
					part = Part.Body;
					continue;
				}

				if (part == Part.Body)
					yield return line;
			}
		}

		public void Dispose()
		{
			if (_disposeReader && _reader != null)
				_reader.Dispose();
		}
	}

	public abstract class BaseIndexingParser : IDocumentParser
	{
		protected int ProviderDocumentIdIndex = 0;
		protected int DocumentDateIndex = 1;

		protected int CodeIndex = -1;
		protected int ProductIndex = -1;
		protected int ProducerIndex = -1;
		protected int CountryIndex = -1;
		protected int QuantityIndex = -1;
		protected int ProducerCostIndex = -1;
		protected int ProducerCostWithoutNdsIndex = -1;
		protected int SupplierCostIndex = -1;
		protected int RegistryDateIndex = -1;
		protected int NdsIndex = -1;
		protected int SupplierPriceMarkupIndex = -1;
		protected int SerialNumberIndex = -1;
		protected int DateOfManufactureIndex = -1;
		protected int ExpireInMonthsIndex = -1;
		protected int PeriodIndex = -1;
		protected int CertificatesIndex = -1;
		protected int CertificateAuthorityIndex = -1;
		protected int RegistryCostIndex = -1;
		protected int VitallyImportantIndex = -1;
		protected int SupplierCostWithoutNdsIndex = -1;
		protected int CertificatesDateIndex = -1;
		protected int AmountIndex = -1;
		protected int NdsAmountIndex = -1;
		protected int UnitIndex = -1;
		protected int ExciseTaxIndex = -1;
		protected int BillOfEntryNumberIndex = -1;
		protected int EAN13Index = -1;

		protected int InvoiceNumberIndex = -1;
		protected int InvoiceDateIndex = -1;
		protected int SellerNameIndex = -1;
		protected int SellerAddressIndex = -1;
		protected int SellerINNIndex = -1;
		protected int SellerKPPIndex = -1;
		protected int ShipperInfoIndex = -1;
		protected int ConsigneeInfoIndex = -1;
		protected int PaymentDocumentInfoIndex = -1;
		protected int BuyerNameIndex = -1;
		protected int BuyerAddressIndex = -1;
		protected int BuyerINNIndex = -1;
		protected int BuyerKPPIndex = -1;
		protected int AmountWithoutNDS0Index = -1;
		protected int AmountWithoutNDS10Index = -1;
		protected int NDSAmount10Index = -1;
		protected int Amount10Index = -1;
		protected int AmountWithoutNDS18Index = -1;
		protected int NDSAmount18Index = -1;
		protected int Amount18Index = -1;
		protected int AmountWithoutNDSIndex = -1;
		protected int InvoiceNDSAmountIndex = -1;
		protected int InvoiceAmountIndex = -1;
		protected int UnitCodeIndex = -1;
		protected int CountryCodeIndex = -1;
		protected int CommissionFeeIndex = -1;
		protected int CommissionFeeContractIdIndex = -1;
		protected int SupplierNameIndex = -1;
		protected int BuyerIdIndex = -1;
		protected int RecipientIdIndex = -1;
		protected int RecipientNameIndex = -1;
		protected int DelayOfPaymentInBankDaysIndex = -1;
		protected int DelayOfPaymentInDaysIndex = -1;

		protected string separator = ";";

		protected string CommentMark;

		protected virtual void SetIndexes()
		{
			ProviderDocumentIdIndex = 0;
			DocumentDateIndex = 1;

			CodeIndex = 0;
			ProductIndex = 1;
			ProducerIndex = 2;
			CountryIndex = 3;
			QuantityIndex = 4;
			ProducerCostWithoutNdsIndex = 5;
			SupplierCostIndex = 6;
			NdsIndex = 7;
			SupplierPriceMarkupIndex = 8;
			SerialNumberIndex = 9;
			PeriodIndex = 10;
			CertificatesIndex = 12;
			RegistryCostIndex = 16;
			VitallyImportantIndex = 18;
			SupplierCostWithoutNdsIndex = -1;
		}

		protected static int? GetInteger(string value)
		{
			if (String.IsNullOrEmpty(value))
				return null;
			int res;
			if (int.TryParse(value, out res))
				return res;
			if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out res))
				return res;
			return null;
		}

		protected static decimal? GetDecimal(string[] parts, int index)
		{
			return GetDecimal(GetString(parts, index));
		}

		protected static decimal? GetDecimal(string value)
		{
			if (String.IsNullOrEmpty(value))
				return null;
			decimal res;
			if (decimal.TryParse(value, out res))
				return res;
			if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out res))
				return res;
			return null;
		}

		private static string GetString(string[] parts, int index)
		{
			if (parts.Length > index && index > 0)
				return GetString(parts[index]);
			return null;
		}

		protected static string GetString(string value)
		{
			return String.IsNullOrEmpty(value) ? null : value;
		}

		protected static bool? GetBool(string value)
		{
			var decimalVal = GetDecimal(value);
			if (decimalVal == null)
				return null;
			int intVal;
			if (!Int32.TryParse(decimalVal.ToString(), out intVal))
				return null;
			if (intVal == 0)
				return false;
			if (intVal == 1)
				return true;
			return null;
		}

		protected static DateTime? GetDateTime(string value)
		{
			var decimalVal = GetString(value);
			if (decimalVal == null)
				return null;
			DateTime intVal;
			if (!DateTime.TryParse(decimalVal, out intVal))
				return null;
			if (intVal != Convert.ToDateTime("01.01.0001 00:00:00"))
				return intVal;
			return null;
		}

		public virtual Document Parse(string file, Document document)
		{
			SetIndexes();

			using (var parser = new HeaderBodyParser(file, CommentMark)) {
				ReadHeader(document, parser.Header().First());
				foreach (var body in parser.Body())
					ReadBody(document, body);
			}

			return document;
		}

		protected void ReadHeader(Document document, string line)
		{
			var header = line.Split(separator.ToCharArray());
			document.ProviderDocumentId = header[ProviderDocumentIdIndex];
			if (!String.IsNullOrEmpty(header[DocumentDateIndex]))
				document.DocumentDate = Convert.ToDateTime(header[DocumentDateIndex]);

			if ((InvoiceNumberIndex >= 0) && header.Length > InvoiceNumberIndex)
				document.SetInvoice().InvoiceNumber = GetString(header[InvoiceNumberIndex]);

			if ((InvoiceDateIndex >= 0) && header.Length > InvoiceDateIndex) {
				if (!String.IsNullOrEmpty(header[InvoiceDateIndex]))
					document.SetInvoice().InvoiceDate = Convert.ToDateTime(header[InvoiceDateIndex]);
			}

			if ((SellerNameIndex >= 0) && header.Length > SellerNameIndex)
				document.SetInvoice().SellerName = GetString(header[SellerNameIndex]);

			if ((SellerAddressIndex >= 0) && header.Length > SellerAddressIndex)
				document.SetInvoice().SellerAddress = GetString(header[SellerAddressIndex]);

			if ((SellerINNIndex >= 0) && header.Length > SellerINNIndex) {
				string inn = GetString(header[SellerINNIndex]);
				if (inn.Contains("/"))
					inn = inn.Split('/')[0];
				document.SetInvoice().SellerINN = inn;
			}

			if ((SellerKPPIndex >= 0) && header.Length > SellerKPPIndex) {
				string kpp = GetString(header[SellerKPPIndex]);
				if (kpp.Contains("/"))
					kpp = kpp.Split('/')[1];
				document.SetInvoice().SellerKPP = kpp;
			}

			if ((ShipperInfoIndex >= 0) && header.Length > ShipperInfoIndex)
				document.SetInvoice().ShipperInfo = GetString(header[ShipperInfoIndex]);

			if ((ConsigneeInfoIndex >= 0) && header.Length > ConsigneeInfoIndex)
				document.SetInvoice().RecipientAddress = GetString(header[ConsigneeInfoIndex]);

			if ((PaymentDocumentInfoIndex >= 0) && header.Length > PaymentDocumentInfoIndex)
				document.SetInvoice().PaymentDocumentInfo = GetString(header[PaymentDocumentInfoIndex]);

			if ((BuyerNameIndex >= 0) && header.Length > BuyerNameIndex)
				document.SetInvoice().BuyerName = GetString(header[BuyerNameIndex]);

			if ((BuyerAddressIndex >= 0) && header.Length > BuyerAddressIndex)
				document.SetInvoice().BuyerAddress = GetString(header[BuyerAddressIndex]);

			if ((BuyerINNIndex >= 0) && header.Length > BuyerINNIndex) {
				string inn = GetString(header[BuyerINNIndex]);
				if (inn.Contains("/"))
					inn = inn.Split('/')[0];
				document.SetInvoice().BuyerINN = inn;
			}

			if ((BuyerKPPIndex >= 0) && header.Length > BuyerKPPIndex) {
				string kpp = GetString(header[BuyerKPPIndex]);
				if (kpp.Contains("/"))
					kpp = kpp.Split('/')[1];
				document.SetInvoice().BuyerKPP = kpp;
			}

			if ((AmountWithoutNDS0Index >= 0) && header.Length > AmountWithoutNDS0Index)
				document.SetInvoice().AmountWithoutNDS0 = GetDecimal(header[AmountWithoutNDS0Index]);

			if ((AmountWithoutNDS10Index >= 0) && header.Length > AmountWithoutNDS10Index)
				document.SetInvoice().AmountWithoutNDS10 = GetDecimal(header[AmountWithoutNDS10Index]);

			if ((NDSAmount10Index >= 0) && header.Length > NDSAmount10Index)
				document.SetInvoice().NDSAmount10 = GetDecimal(header[NDSAmount10Index]);

			if ((Amount10Index >= 0) && header.Length > Amount10Index)
				document.SetInvoice().Amount10 = GetDecimal(header[Amount10Index]);

			if ((AmountWithoutNDS18Index >= 0) && header.Length > AmountWithoutNDS18Index)
				document.SetInvoice().AmountWithoutNDS18 = GetDecimal(header[AmountWithoutNDS18Index]);

			if ((NDSAmount18Index >= 0) && header.Length > NDSAmount18Index)
				document.SetInvoice().NDSAmount18 = GetDecimal(header[NDSAmount18Index]);

			if ((Amount18Index >= 0) && header.Length > Amount18Index)
				document.SetInvoice().Amount18 = GetDecimal(header[Amount18Index]);

			if ((AmountWithoutNDSIndex >= 0) && header.Length > AmountWithoutNDSIndex)
				document.SetInvoice().AmountWithoutNDS = GetDecimal(header[AmountWithoutNDSIndex]);

			if ((InvoiceNDSAmountIndex >= 0) && header.Length > InvoiceNDSAmountIndex)
				document.SetInvoice().NDSAmount = GetDecimal(header[InvoiceNDSAmountIndex]);

			if ((InvoiceAmountIndex >= 0) && header.Length > InvoiceAmountIndex)
				document.SetInvoice().Amount = GetDecimal(header[InvoiceAmountIndex]);

			if ((CommissionFeeIndex >= 0) && header.Length > CommissionFeeIndex)
				document.SetInvoice().CommissionFee = GetDecimal(header[CommissionFeeIndex]);

			if ((CommissionFeeContractIdIndex >= 0) && header.Length > CommissionFeeContractIdIndex)
				document.SetInvoice().CommissionFeeContractId = GetString(header[CommissionFeeContractIdIndex]);

			if ((BuyerIdIndex >= 0) && header.Length > BuyerIdIndex)
				document.SetInvoice().BuyerId = GetInteger(header[BuyerIdIndex]);

			if ((RecipientIdIndex >= 0) && header.Length > RecipientIdIndex)
				document.SetInvoice().RecipientId = GetInteger(header[RecipientIdIndex]);

			if ((RecipientNameIndex >= 0) && header.Length > RecipientNameIndex)
				document.SetInvoice().RecipientName = GetString(header[RecipientNameIndex]);

			if ((DelayOfPaymentInBankDaysIndex >= 0) && header.Length > DelayOfPaymentInBankDaysIndex)
				document.SetInvoice().DelayOfPaymentInBankDays = GetInteger(header[DelayOfPaymentInBankDaysIndex]);

 			if ((DelayOfPaymentInDaysIndex >= 0) && header.Length > DelayOfPaymentInDaysIndex)
				document.SetInvoice().DelayOfPaymentInDays = GetInteger(header[DelayOfPaymentInDaysIndex]);

		}

		protected virtual void ReadBody(Document document, string line)
		{
			var parts = line.Split(separator.ToCharArray());
			var docLine = document.NewLine();

			if (CodeIndex >= 0)
				docLine.Code = parts[CodeIndex];
			docLine.Product = parts[ProductIndex];
			docLine.Producer = parts[ProducerIndex];
			docLine.Country = GetString(parts, CountryIndex);
			docLine.Quantity = (uint?)GetDecimal(parts[QuantityIndex]);
			docLine.Unit = GetString(parts, UnitIndex);
			docLine.ProducerCostWithoutNDS = GetDecimal(parts, ProducerCostWithoutNdsIndex);
			docLine.SupplierCost = GetDecimal(parts, SupplierCostIndex);
			docLine.RegistryDate = GetDateTime(GetString(parts, RegistryDateIndex));
			docLine.Nds = (uint?)GetDecimal(parts, NdsIndex);
			docLine.SupplierPriceMarkup = GetDecimal(parts, SupplierPriceMarkupIndex);
			docLine.SupplierCostWithoutNDS = GetDecimal(parts, SupplierCostWithoutNdsIndex);
			docLine.ExciseTax = GetDecimal(parts, ExciseTaxIndex);
			docLine.SerialNumber = GetString(parts, SerialNumberIndex);
			docLine.DateOfManufacture = GetDateTime(GetString(parts, DateOfManufactureIndex));
			docLine.ExpireInMonths = GetInteger(GetString(parts, ExpireInMonthsIndex));
			docLine.Period = GetString(parts, PeriodIndex);
			docLine.Certificates = GetString(parts, CertificatesIndex);
			docLine.CertificateAuthority = GetString(parts, CertificateAuthorityIndex);
			docLine.CertificatesDate = GetString(parts, CertificatesDateIndex);
			docLine.RegistryCost = GetDecimal(parts, RegistryCostIndex);
			docLine.BillOfEntryNumber = GetString(parts, BillOfEntryNumberIndex);

			if ((VitallyImportantIndex > 0) && parts.Length > VitallyImportantIndex && !String.IsNullOrEmpty(parts[VitallyImportantIndex])) {
				docLine.VitallyImportant = GetBool(parts[VitallyImportantIndex]);
				if (parts[VitallyImportantIndex].ToLower() == "да")
					docLine.VitallyImportant = true;
				if (parts[VitallyImportantIndex].ToLower() == "нет")
					docLine.VitallyImportant = false;
			}

			docLine.EAN13 = GetString(parts, EAN13Index);
			docLine.CountryCode = GetString(parts, CountryCodeIndex);
			docLine.UnitCode = GetString(parts, UnitCodeIndex);
			docLine.Amount = GetDecimal(parts, AmountIndex);
			docLine.NdsAmount = GetDecimal(parts, NdsAmountIndex);
			docLine.ProducerCost = GetDecimal(parts, ProducerCostIndex);
		}

		public static bool CheckByHeaderPart(string file, IEnumerable<string> name, string commentMark)
		{
			if (!Path.GetExtension(file).Match(".txt"))
				return false;

			using (var parser = new HeaderBodyParser(file, commentMark)) {
				var header = parser.Header().FirstOrDefault();
				if (header == null)
					return false;
				var parts = header.Split(';');
				if (parts.Length < 4)
					return false;
				if (name.All(n => !parts[3].Equals(n, StringComparison.CurrentCultureIgnoreCase)))
					return false;
				if (GetString(parts[4]) != null && GetString(parts[4]).Contains("ЛИПЕЦК, *ЛИПЕЦКФАРМАЦИЯ Аптека"))
					return false;

				var line = parser.Body().FirstOrDefault();
				if (line == null)
					return false;

				parts = line.Split(';');
				if (GetDecimal(parts[6]) == null)
					return false;
				return true;
			}
		}

		public static bool CheckByHeaderPart(string file, IEnumerable<string> name)
		{
			return CheckByHeaderPart(file, name, null);
		}
	}
}