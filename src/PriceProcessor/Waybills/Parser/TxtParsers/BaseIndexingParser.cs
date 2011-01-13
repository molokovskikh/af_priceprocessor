using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

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
			while ((line = _reader.ReadLine()) != null)
			{
				yield return line;
			}
		}

		public IEnumerable<string> Header()
		{
			foreach (var line in Lines().Where(l => !String.IsNullOrWhiteSpace(l)).Where(l => String.IsNullOrEmpty(_commentMark) || !l.StartsWith(_commentMark)))
			{
				if (part == Part.None && line.ToLower() == "[header]")
				{
					part = Part.Header;
					continue;
				}

				if (part == Part.Header)
					yield return line;
			}
		}

		public IEnumerable<string> Body()
		{
			foreach (var line in Lines().Where(l => !String.IsNullOrWhiteSpace(l)).Where(l => String.IsNullOrEmpty(_commentMark) || !l.StartsWith(_commentMark)))
			{
				if (part == Part.Header && line.ToLower() == "[body]")
				{
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
		protected int SupplierCostIndex = -1;
		protected int NdsIndex = -1;
		protected int SupplierPriceMarkupIndex = -1;
		protected int SerialNumberIndex = -1;
		protected int PeriodIndex = -1;
		protected int CertificatesIndex = -1;
		protected int RegistryCostIndex = -1;
		protected int VitallyImportantIndex = -1;
		protected int SupplierCostWithoutNdsIndex = -1;

		protected string CommentMark;
		protected bool CalculateSupplierPriceMarkup;

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

			using(var parser = new HeaderBodyParser(file, CommentMark))
			{
				ReadHeader(document, parser.Header().First());
				foreach (var body in parser.Body())
					ReadBody(document, body);
			}

			return document;
		}

		private void ReadHeader(Document document, string line)
		{
			var header = line.Split(';');
			document.ProviderDocumentId = header[ProviderDocumentIdIndex];
			if (!String.IsNullOrEmpty(header[DocumentDateIndex]))
				document.DocumentDate = Convert.ToDateTime(header[DocumentDateIndex]);
		}

		private void ReadBody(Document document, string line)
		{
			var parts = line.Split(';');
			var docLine = document.NewLine();

			docLine.Code = parts[CodeIndex];
			docLine.Product = parts[ProductIndex];
			docLine.Producer = parts[ProducerIndex];
			docLine.Country = GetString(parts[CountryIndex]);
			docLine.Quantity = Convert.ToUInt32(GetDecimal(parts[QuantityIndex]));

			if ((ProducerCostIndex > 0) && parts.Length > ProducerCostIndex)
				docLine.ProducerCost = GetDecimal(parts[ProducerCostIndex]);

			if (SupplierCostIndex > 0)
				docLine.SupplierCost = GetDecimal(parts[SupplierCostIndex]);

			if ((NdsIndex > 0) && (parts.Length > NdsIndex))
				docLine.Nds = (uint?)GetDecimal(parts[NdsIndex]);

			if ((SupplierPriceMarkupIndex > 0) && (parts.Length > SupplierPriceMarkupIndex))
				docLine.SupplierPriceMarkup = GetDecimal(parts[SupplierPriceMarkupIndex]);

			if ((SupplierCostWithoutNdsIndex > 0) && (parts.Length > SupplierCostWithoutNdsIndex))
				docLine.SupplierCostWithoutNDS = GetDecimal(parts[SupplierCostWithoutNdsIndex]);

			if ((SerialNumberIndex > 0) && (parts.Length>SerialNumberIndex))
			docLine.SerialNumber = GetString(parts[SerialNumberIndex]);

			if ((SerialNumberIndex > 0) && (parts.Length > SerialNumberIndex))
			docLine.Period = GetString(parts[PeriodIndex]);

			if ((CertificatesIndex > 0) && (parts.Length > CertificatesIndex))
				docLine.Certificates = GetString(parts[CertificatesIndex]);

			if ((RegistryCostIndex > 0) && (parts.Length > RegistryCostIndex))
				docLine.RegistryCost = GetDecimal(parts[RegistryCostIndex]);

			if ((VitallyImportantIndex > 0) && parts.Length > VitallyImportantIndex && !String.IsNullOrEmpty(parts[VitallyImportantIndex]))
				docLine.VitallyImportant = GetBool(parts[VitallyImportantIndex]);

			if (CalculateSupplierPriceMarkup) 
				docLine.SetSupplierPriceMarkup();
			docLine.SetValues();
		}

		public static bool CheckByHeaderPart(string file, IEnumerable<string> name, string commentMark)
		{
			if (Path.GetExtension(file).ToLower() != ".txt")
				return false;

			using (var parser = new HeaderBodyParser(file, commentMark))
			{
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
