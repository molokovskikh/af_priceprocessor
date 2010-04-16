using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	class ProtekDocumentHeader
	{
		public string[] DocumentDateHeaders = { "Дата", "DATE0", "Дата документа" };
		public string[] ProviderDocumentIdHeaders = { "CD_A", "Номер документа", "Номер" };
		public string[] CodeHeaders = { "CD_M", "Код товара" };
		public string[] ProductHeaders = { "NM_M", "Наименование товара" };
		public string[] ProducerHeaders = { "NM_PROD", "Производитель" };
		public string[] CountryHeaders = { "COUNTRY", "Страна производителя" };
		public string[] QuantityHeaders = { "QTY", "Количество" };
		public string[] SupplierCostHeaders = { "RRPRICE", "Цена с НДС" };
		public string[] SupplierCostWithoutNdsHeaders = { "DISTR_PRICE_WONDS", "Цена Протека без НДС", "Цена поставщика без НДС" };
		public string[] CertificatesHeaders = { "SSERIA", "SERIA", "Серии сертификатов", "Сертификаты" };
		public string[] SerialNumberHeaders = { "PRODSERIA", "Серия производителя" };
		public string[] ProducerCostHeaders = { "PROD_PRICE_WONDS", "Цена производителя без НДС", "Цена производителя" };
		public string[] RegistryCostHeaders = { "PRICERUB", "Реестровая цена в рублях", "Реестровая цена" };
		public string[] NdsHeaders = { "PROC_NDS" };
		public string[] PeriodHeaders = { "EXPIRY", "Дата истекания срока годности данной серии", "Дата окончания срока годности серии" };
		public string[] SupplierPriceMarkupHeaders = { "Наценка посредника", "Торговая надбавка оптового звена" };
		public string[] VitallyImportantHeaders = { "Признак ЖВНЛС", "ЖНВЛС" };

		private IList<string> _headerParts;

		public ProtekDocumentHeader(string headerLine, char separator)
		{
			_headerParts = headerLine.Split(separator);
			UseSequencedIndexing = false;
		}

		public ProtekDocumentHeader(string[] headerParts)
		{
			_headerParts = headerParts;
			UseSequencedIndexing = false;
		}

		public bool UseSequencedIndexing { get; set; }

		private decimal? GetDecimal(int index, string[] body)
		{
			try
			{
				if (index >= 0 && !String.IsNullOrEmpty(body[index]))
					return (Convert.ToDecimal(body[index], CultureInfo.InvariantCulture));
			}
			catch (Exception) {}
			return null;
		}

		private uint? GetUInt(int index, string[] body)
		{
			try
			{
				if (index >= 0 && !String.IsNullOrEmpty(body[index]))
					return (uint.Parse(body[index], NumberStyles.Any, CultureInfo.InvariantCulture));
			}
			catch (Exception) {}
			return null;			
		}

		private bool? GetBoolean(int index, string[] body)
		{
			try
			{
				if (index >= 0 && !String.IsNullOrEmpty(body[index]))
					return (Convert.ToBoolean(Convert.ToUInt32(body[index], CultureInfo.InvariantCulture)));
			}
			catch (Exception) {}
			return null;						
		}

		private DateTime? GetDateTime(int index, string[] body)
		{
			try
			{
				if (index >= 0 && !String.IsNullOrEmpty(body[index]))
					return (Convert.ToDateTime(body[index]));
			}
			catch (Exception) {}
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

		private int GetIndexOfAnyElement(IList<string> list, IEnumerable<string> elements)
		{
			foreach (var element in elements)
			{
				if (list.Contains(element))
				{
					//return list.IndexOf(element) < 2 ? 0 : (list.IndexOf(element) + 1) / 3 - 1;
					if (UseSequencedIndexing)
						return list.IndexOf(element) == 0 ? 0 : list.IndexOf(element);
					return list.IndexOf(element) == 0 ? 0 : list.IndexOf(element)/3;
				}
			}
			return -1;
		}
	}
}
