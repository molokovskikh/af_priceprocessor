using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
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
		protected int SupplierCostIndex = -1;
		protected int NdsIndex = -1;
		protected int SupplierPriceMarkupIndex = -1;
		protected int SerialNumberIndex = -1;
		protected int PeriodIndex = -1;
		protected int CertificatesIndex = -1;
		protected int RegistryCostIndex = -1;
		protected int VitallyImportantIndex = -1;
		protected int SupplierCostWithoutNdsIndex = -1;

		protected virtual void SetIndexes()
		{
			ProviderDocumentIdIndex = 0;
			DocumentDateIndex = 1;

			CodeIndex = 0;
			ProductIndex = 1;
			ProducerIndex = 2;
			CountryIndex = 3;
			QuantityIndex = 4;
			ProducerCostIndex = 5;
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

		private static string GetString(string value)
		{
			return String.IsNullOrEmpty(value) ? null : value;
		}

		private static bool? GetBool(string value)
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

		public virtual Document Parse(string file, Document document)
		{
			SetIndexes();
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				reader.ReadLine();
				var header = reader.ReadLine().Split(';');
				document.ProviderDocumentId = header[ProviderDocumentIdIndex];
				if (!String.IsNullOrEmpty(header[DocumentDateIndex]))
					document.DocumentDate = Convert.ToDateTime(header[DocumentDateIndex]);

				reader.ReadLine();
				var bodyLine = String.Empty;
				while ((bodyLine = reader.ReadLine()) != null)
				{
					if (String.IsNullOrEmpty(bodyLine))
						continue;
					var line = bodyLine.Split(';');
					var docLine = document.NewLine();

					docLine.Code = line[CodeIndex];
					docLine.Product = line[ProductIndex];
					docLine.Producer = line[ProducerIndex];
					docLine.Country = GetString(line[CountryIndex]);
					docLine.Quantity = Convert.ToUInt32(GetDecimal(line[QuantityIndex]));

					if ((ProducerCostIndex > 0) && line.Length > ProducerCostIndex)
						docLine.ProducerCost = GetDecimal(line[ProducerCostIndex]);

					if (SupplierCostIndex > 0)
						docLine.SupplierCost = GetDecimal(line[SupplierCostIndex]);

					if ((NdsIndex > 0) && (line.Length > NdsIndex))
						docLine.Nds = (uint?)GetDecimal(line[NdsIndex]);

					if ((SupplierPriceMarkupIndex > 0) && (line.Length > SupplierPriceMarkupIndex))
						docLine.SupplierPriceMarkup = GetDecimal(line[SupplierPriceMarkupIndex]);

					if ((SupplierCostWithoutNdsIndex > 0) && (line.Length > SupplierCostWithoutNdsIndex))
						docLine.SupplierCostWithoutNDS = GetDecimal(line[SupplierCostWithoutNdsIndex]);

					docLine.SerialNumber = GetString(line[SerialNumberIndex]);
					docLine.Period = GetString(line[PeriodIndex]);

					if ((CertificatesIndex > 0) && (line.Length > CertificatesIndex))
						docLine.Certificates = GetString(line[CertificatesIndex]);

					if ((RegistryCostIndex > 0) && (line.Length > RegistryCostIndex))
						docLine.RegistryCost = GetDecimal(line[RegistryCostIndex]);

					if ((VitallyImportantIndex > 0) && line.Length > VitallyImportantIndex && !String.IsNullOrEmpty(line[VitallyImportantIndex]))
						docLine.VitallyImportant = GetBool(line[VitallyImportantIndex]);

					docLine.SetValues();
				}
			}
			return document;
		}

		public static bool CheckByHeaderPart(string file, IEnumerable<string> name)
		{
			if (Path.GetExtension(file).ToLower() != ".txt")
				return false;
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var headerCaption = reader.ReadLine();
				if (!headerCaption.ToLower().Equals("[header]"))
					return false;
				var header = reader.ReadLine().Split(';');
				if (header.Length < 4)
					return false;
				if (name.All(n => !header[3].Equals(n, StringComparison.CurrentCultureIgnoreCase)))
					return false;
				var bodyCaption = reader.ReadLine();
				if (!bodyCaption.ToLower().Equals("[body]"))
					return false;
				var body = reader.ReadLine().Split(';');
				if (GetDecimal(body[6]) == null)
					return false;
			}
			return true;
		}
	}
}
