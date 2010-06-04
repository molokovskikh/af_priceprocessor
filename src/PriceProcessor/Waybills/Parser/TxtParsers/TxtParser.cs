using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Parser.Helpers;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class TxtParser
	{
		private string[] DocumentDateHeaders = { "Дата" };
		private string[] ProviderDocumentIdHeaders = { "Фактура" };
		private string[] CodeHeaders = { "Код" };
		private string[] ProductHeaders = { "Наименование товара" };
		private string[] ProducerHeaders = { "Про-ль" };
		private string[] CountryHeaders = {  "Страна производителя" };
		private string[] QuantityHeaders = { "Кол-во" };
		private string[] SupplierCostHeaders = { "Цена с НДС" };
		private string[] SupplierCostWithoutNdsHeaders = { "Цена без НДС" };
		private string[] CertificatesHeaders = { "Номер сертификата" };
		private string[] SerialNumberHeaders = { "Серия" };
		private string[] ProducerCostHeaders = { "Цена з/и без НДС" };
		private string[] RegistryCostHeaders = { "Реестр" };
		private string[] NdsHeaders = { };
		private string[] PeriodHeaders = { "Срок годности" };
		private string[] SupplierPriceMarkupHeaders = { };
		private string[] VitallyImportantHeaders = { "ЖВ" };

		private List<string> _headers;
		private string[] _values;
		private char _separator;

		public TxtParser(string header, char separator)
		{
			_separator = separator;
			_headers = header.Split(_separator).ToList();
		}

		public bool ReadLine(StreamReader reader)
		{
			var line = reader.ReadLine();
			if (line == null)
				return false;
			_values = line.Split(_separator);
			return true;
		}

		private int GetIndex(IEnumerable<string> headerNames)
		{
			foreach (var name in headerNames)
			{
				var index = _headers.IndexOf(name);
				if (index >= 0)
					return index;
			}
			return -1;
		}

		private string GetValue(IEnumerable<string> headerNames)
		{
			var index = GetIndex(headerNames);
			if (index >= 0 && index < _values.Length)
				return _values[index];
			return null;
		}

		public DateTime? DocumentDate
		{
			get { return ParseHelper.GetDateTime(GetValue(DocumentDateHeaders)); }
		}

		public string ProviderDocumentId
		{
			get { return ParseHelper.GetString(GetValue(ProviderDocumentIdHeaders)); }
		}

		public string Code
		{
			get { return ParseHelper.GetString(GetValue(CodeHeaders)); }
		}

		public string Product
		{
			get { return ParseHelper.GetString(GetValue(ProductHeaders)); }
		}

		public string Producer
		{
			get { return ParseHelper.GetString(GetValue(ProducerHeaders)); }
		}

		public string Country
		{
			get { return ParseHelper.GetString(GetValue(CountryHeaders)); }
		}

		public uint? Quantity
		{
			get { return ParseHelper.GetUInt(GetValue(QuantityHeaders)); }
		}

		public decimal? SupplierCost
		{
			get { return ParseHelper.GetDecimal(GetValue(SupplierCostHeaders)); }
		}

		public decimal? SupplierCostWithoutNds
		{
			get { return ParseHelper.GetDecimal(GetValue(SupplierCostWithoutNdsHeaders)); }
		}

		public string Certificates
		{
			get { return ParseHelper.GetString(GetValue(CertificatesHeaders)); }
		}

		public string SerialNumber
		{
			get { return ParseHelper.GetString(GetValue(SerialNumberHeaders)); }
		}

		public decimal? ProducerCost
		{
			get { return ParseHelper.GetDecimal(GetValue(ProducerCostHeaders)); }
		}

		public decimal? RegistryCost
		{
			get { return ParseHelper.GetDecimal(GetValue(RegistryCostHeaders)); }
		}

		public uint? Nds
		{
			get { return ParseHelper.GetUInt(GetValue(NdsHeaders)); }
		}

		public string Period
		{
			get { return ParseHelper.GetString(GetValue(PeriodHeaders)); }
		}

		public bool? VitallyImportant
		{
			get { return ParseHelper.GetBoolean(GetValue(VitallyImportantHeaders)); }
		}

		public string VitallyImportantChar
		{
			get { return ParseHelper.GetString(GetValue(VitallyImportantHeaders)); }
		}

		public decimal? SupplierPriceMarkup
		{
			get { return ParseHelper.GetDecimal(GetValue(SupplierPriceMarkupHeaders)); }
		}
	}
}
