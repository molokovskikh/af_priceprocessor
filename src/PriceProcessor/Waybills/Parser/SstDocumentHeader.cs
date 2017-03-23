using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	internal class SstDocumentHeader
	{
		public string[] DocumentDateHeaders = { "Дата", "DATE0", "Дата документа", "Дата оформления" };
		public string[] ProviderDocumentIdHeaders = { "CD_A", "Номер документа", "Номер", "Код накладной" };
		public string[] CodeHeaders = { "CD_M", "Код товара", "Код препарата в ЦВ Протек" };
		public string[] ProductHeaders = { "NM_M", "Наименование товара", "Название препарата в верхнем регистре" };
		public string[] ProducerHeaders = { "NM_PROD", "Производитель", "Название производителя препарата" };
		public string[] CountryHeaders = { "COUNTRY", "Страна производителя", "Название страны производителя" };
		public string[] QuantityHeaders = { "QTY", "Количество" };
		public string[] SupplierCostHeaders = { "RRPRICE", "Цена с НДС", "Итоговая цена", "Резерв" };
		public string[] SupplierCostWithoutNdsHeaders = { "DISTR_PRICE_WONDS", "Цена Протека без НДС", "Цена поставщика без НДС", "Цена АХ без НДС" };
		public string[] CertificatesHeaders = { "SSERIA", "SERIA", "Серии сертификатов", "Сертификаты", "Сертификаты (Серия^Регистрационный номер^Дата и орган, выдавший сертификат)" };
		public string[] SerialNumberHeaders = { "PRODSERIA", "Серия производителя" };
		public string[] ProducerCostHeaders = { "PROD_PRICE_WONDS", "Цена производителя без НДС", "Цена производителя" };
		public string[] RegistryCostHeaders = { "PRICERUB", "Реестровая цена в рублях", "Реестровая цена" };
		public string[] RegistryDateHeaders = { "Дата регистрации препарата в реестре" };
		public string[] NdsHeaders = { "PROC_NDS" };
		public string[] PeriodHeaders = { "EXPIRY", "Дата истекания срока годности данной серии", "Дата окончания срока годности серии" };
		public string[] SupplierPriceMarkupHeaders = { "Наценка посредника", "Торговая надбавка оптового звена" };
		public string[] VitallyImportantHeaders = { "Признак ЖВНЛС", "Признак ЖНВЛС", "ЖНВЛС", "Признак ЖВЛС" };
		public string[] EAN13Headers = { "Штрих-код производителя" };
		public string[] BillOfEntryNumberHeaders = { "ГТД" };
		public string[] DateOfManufactureHeaders = { "Дата выпуска препарата" };
		public string[] ExpireInMonthsHeaders = { "Заводской срок годности" , "Заводской срок годности в месяцах" };
		public string[] AmountHeaders = { "Суммарная стоимость", "Стоимость позиции" };
		public string[] NDSAmount10Headers = { "Сумма НДС 10%" };
		public string[] NDSAmount18Headers = { "Сумма НДС 20%" };
		public string[] RecipientIdHeaders = { "Код получателя" };

		private IList<string> _headerParts;

		public SstDocumentHeader(string headerLine, char separator)
		{
			_headerParts = headerLine.Split(separator);
			UseSequencedIndexing = false;
		}

		public SstDocumentHeader(string[] headerParts)
		{
			_headerParts = headerParts;
			UseSequencedIndexing = false;
		}

		public bool UseSequencedIndexing { get; set; }

		private decimal? GetDecimal(int index, string[] body)
		{
			decimal value;
			if (index >= 0 &&
				!String.IsNullOrEmpty(body[index]) &&
				decimal.TryParse(body[index], NumberStyles.Number, CultureInfo.CurrentCulture, out value))
				return value;
			if (index >= 0 &&
				!String.IsNullOrEmpty(body[index]) &&
				decimal.TryParse(body[index], NumberStyles.Number, CultureInfo.InvariantCulture, out value))
				return value;
			return null;
		}

		private uint? GetUInt(int index, string[] body)
		{
			var value = GetDecimal(index, body);
			if (value.HasValue)
				return Convert.ToUInt32(value);
			uint value2;
			if (index >= 0 &&
				!String.IsNullOrEmpty(body[index]) &&
				uint.TryParse(body[index], out value2))
				return value2;
			return null;
		}

		private bool? GetBoolean(int index, string[] body)
		{
			int value;
			if (index >= 0 &&
				!String.IsNullOrEmpty(body[index]) &&
				int.TryParse(body[index], NumberStyles.Any, CultureInfo.InvariantCulture, out value))
				return (value != 0);
			return null;
		}

		private DateTime? GetDateTime(int index, string[] body)
		{
			DateTime value;
			if (index >= 0 &&
				!String.IsNullOrEmpty(body[index]) &&
				DateTime.TryParse(body[index], out value))
				return value;
			return null;
		}

		private string GetString(int index, string[] body)
		{
			if (index >= 0 && !String.IsNullOrEmpty(body[index]))
				return body[index];
			return null;
		}

		public DateTime? GetDocumentDate(string[] headerCaptions, string[] header)
		{
			var index = GetIndexOfAnyElement(headerCaptions, DocumentDateHeaders);
			return GetDateTime(index, header);
		}

		public DateTime? GetDocumentDate(string[] headerData)
		{
			var index = GetIndexOfAnyElement(_headerParts, DocumentDateHeaders);
			return GetDateTime(index, headerData);
		}

		public string GetProviderDocumentId(string[] headerCaptions, string[] header)
		{
			var index = GetIndexOfAnyElement(headerCaptions, ProviderDocumentIdHeaders);
			return GetString(index, header);
		}

		public string GetProviderDocumentId(string[] headerData)
		{
			var index = GetIndexOfAnyElement(_headerParts, ProviderDocumentIdHeaders);
			return GetString(index, headerData);
		}

		public string GetCode(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, CodeHeaders);
			return GetString(index, body);
		}

		public string GetProduct(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, ProductHeaders);
			return GetString(index, body);
		}

		public string GetProducer(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, ProducerHeaders);
			return GetString(index, body);
		}

		public string GetCountry(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, CountryHeaders);
			return GetString(index, body);
		}

		public uint? GetQuantity(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, QuantityHeaders);
			return GetUInt(index, body);
		}

		public decimal? GetSupplierCost(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, SupplierCostHeaders);
			var supplierCost = GetDecimal(index, body);
			if (supplierCost.HasValue)
				return supplierCost;
			return null;
		}

		public decimal? GetSupplierCostWithoutNds(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, SupplierCostWithoutNdsHeaders);
			var supplierCostWithoutNds = GetDecimal(index, body);
			if (supplierCostWithoutNds.HasValue)
				return supplierCostWithoutNds;
			return null;
		}

		public string GetCertificates(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, CertificatesHeaders);
			return GetString(index, body);
		}

		public decimal? GetProducerCost(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, ProducerCostHeaders);
			return GetDecimal(index, body);
		}

		public decimal? GetRegistryCost(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, RegistryCostHeaders);
			return GetDecimal(index, body);
		}

		public DateTime? GetRegistryDate(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, RegistryDateHeaders);
			return GetDateTime(index, body);
		}

		public decimal? GetSupplierPriceMarkup(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, SupplierPriceMarkupHeaders);
			return GetDecimal(index, body);
		}

		public uint? GetNds(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, NdsHeaders);
			var nds = GetUInt(index, body);
			if (nds.HasValue)
				return nds;
			return null;
		}

		public bool? GetVitallyImportant(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, VitallyImportantHeaders);
			return GetBoolean(index, body);
		}

		public string GetPeriod(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, PeriodHeaders);
			return GetString(index, body);
		}

		public string GetSerialNumber(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, SerialNumberHeaders);
			return GetString(index, body);
		}

		public string GetEAN13(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, EAN13Headers);
			return GetString(index, body);
		}

		public string GetBillOfEntryNumber(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, BillOfEntryNumberHeaders);
			return GetString(index, body);
		}

		public DateTime? GetDateOfManufacture(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, DateOfManufactureHeaders);
			return GetDateTime(index, body);
		}

		public int? GetExpireInMonths(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, ExpireInMonthsHeaders);
			var value = GetUInt(index, body);
			if (value.HasValue)
				return (int)value;
			return null;
		}

		public int? GetRecipientId(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, RecipientIdHeaders);
			var value = GetUInt(index, body);
			if (value.HasValue)
				return (int)value;
			return null;
		}

		public decimal? GetAmount(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, AmountHeaders);
			return GetDecimal(index, body);
		}

		public decimal? GetNDSAmount10(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, NDSAmount10Headers);
			return GetDecimal(index, body);
		}

		public decimal? GetNDSAmount18(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, NDSAmount18Headers);
			return GetDecimal(index, body);
		}

		private int GetIndexOfAnyElement(IList<string> list, IEnumerable<string> elements)
		{
			foreach (var element in elements) {
				if (list.Contains(element)) {
					//return list.IndexOf(element) < 2 ? 0 : (list.IndexOf(element) + 1) / 3 - 1;
					if (UseSequencedIndexing)
						return list.IndexOf(element) == 0 ? 0 : list.IndexOf(element);
					return list.IndexOf(element) == 0 ? 0 : list.IndexOf(element) / 3;
				}
			}
			return -1;
		}
	}
}