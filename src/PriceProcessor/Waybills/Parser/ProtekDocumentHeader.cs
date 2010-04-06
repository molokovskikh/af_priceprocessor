using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	class ProtekDocumentHeader
	{
		public string[] DocumentDateHeaders = { "Дата", "DATE0" };
		public string[] ProviderDocumentIdHeaders = { "CD_A" };
		public string[] CodeHeaders = { "CD_M" };
		public string[] ProductHeaders = { "NM_M" };
		public string[] ProducerHeaders = { "NM_PROD" };
		public string[] CountryHeaders = { "COUNTRY" };
		public string[] QuantityHeaders = { "QTY" };
		public string[] SupplierCostHeaders = { "RRPRICE" };
		public string[] SupplierCostWithoutNdsHeaders = { "DISTR_PRICE_WONDS" };
		public string[] CertificatesHeaders = { "SSERIA", "SERIA" };
		public string[] ProducerCostHeaders = { "PROD_PRICE_WONDS" };
		public string[] RegistryCostHeaders = { "PRICERUB" };
		public string[] NdsHeaders = { "PROC_NDS" };
		public string[] PeriodHeaders = { "EXPIRY" };
		public string[] VitallyImportantHeaders = { "Признак ЖВНЛС" };

		private IList<string> _headerParts;

		public ProtekDocumentHeader(string headerLine, char separator)
		{
			_headerParts = headerLine.Split(separator);
		}

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

		public string GetProviderDocumentId(string[] headerCaptions, string[] header)
		{
			var index = GetIndexOfAnyElement(headerCaptions, ProviderDocumentIdHeaders);
			return GetString(index, header);
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
			var nds = GetNds(body);
			var supplierCostWithoutNds = GetSupplierCostWithoutNds(body);
			return Math.Round(supplierCostWithoutNds.Value * (1 + ((decimal)nds.Value / 100)), 2);
		}

		public decimal? GetSupplierCostWithoutNds(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, SupplierCostWithoutNdsHeaders);
			var supplierCostWithoutNds = GetDecimal(index, body);
			if (supplierCostWithoutNds.HasValue)
				return supplierCostWithoutNds;
			var nds = GetNds(body);
			var supplierCost = GetSupplierCost(body);
			return Math.Round(supplierCost.Value / (1 + ((decimal)nds.Value) / 100), 2);
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

		public uint? GetNds(string[] body)
		{
			var index = GetIndexOfAnyElement(_headerParts, NdsHeaders);
			var nds = GetUInt(index, body);
			if (nds.HasValue)
				return nds;
			var supplierCost = GetSupplierCost(body);
			var supplierCostWithoutNDS = GetSupplierCostWithoutNds(body);
			return (uint?)(Math.Round((supplierCost.Value / supplierCostWithoutNDS.Value - 1) * 100));
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

		private static int GetIndexOfAnyElement(IList<string> list, IEnumerable<string> elements)
		{
			foreach (var element in elements)
			{
				if (list.Contains(element))
					//return list.IndexOf(element) < 2 ? 0 : (list.IndexOf(element) + 1) / 3 - 1;
					return list.IndexOf(element) == 0 ? 0 : list.IndexOf(element) / 3;
			}
			return -1;
		}
	}
}
