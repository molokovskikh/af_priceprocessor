using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Parser.TxtParsers;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class UkonParser : IDocumentParser
	{
		// Символ, который обозначает комментарий
		private const char CommentSymbol = '-';

		private const string HeaderCaption = "[HEADER]";

		private const string BodyCaption = "[BODY]";

		private const string StartLineForHeaders = "В следующей строке перечислены";

		private static bool isCorrectBody;

		private static bool IsCommentLine(string line)
		{
			return (!String.IsNullOrEmpty(line)) && line[0].Equals(CommentSymbol);
		}

		private static bool IsHeaderCaption(string line)
		{
			return !String.IsNullOrEmpty(line) && line.ToLower().Equals(HeaderCaption.ToLower());
		}

		private static bool IsBodyCaption(string line)
		{
			return !String.IsNullOrEmpty(line) && line.ToLower().Equals(BodyCaption.ToLower());
		}

		private static bool IsHeaderBeforeData(string line, string headerName)
		{
			return !String.IsNullOrEmpty(line) && line.ToLower().Equals(headerName.ToLower());
		}

		private static bool IsStartLineForHeaders(string line)
		{
			return line.ToLower().Contains(StartLineForHeaders.ToLower());
		}

		private static ProtekDocumentHeader ReadHeader(StreamReader reader, string headerName)
		{
			var line = reader.ReadLine();
			while ((line != null) && IsCommentLine(line) && !IsStartLineForHeaders(line))
				line = reader.ReadLine();
			if (!IsStartLineForHeaders(line))
			{
				isCorrectBody = false;
				return null;
			}	
			var headerParts = new List<string>();
			while ((line != null) && !IsHeaderBeforeData(line, headerName))
			{
				line = reader.ReadLine();
				if (line == null)
					return null;
				headerParts.AddRange(line.Split(';'));
			}
			if (headerParts.Count == 0)
				return null;
			for (var i = 0; i < headerParts.Count; i++)
			{
				if (String.IsNullOrEmpty(headerParts[i]))
				{
					headerParts.RemoveAt(i--);
					continue;
				}
				headerParts[i] = headerParts[i].Trim('-', ' ');
			}
			return new ProtekDocumentHeader(headerParts.ToArray()) { UseSequencedIndexing = true };
		}

		private static string ReadSimpleHeader(StreamReader reader, string headerName)
		{
			var line = reader.ReadLine();
			while (IsCommentLine(line) && !IsHeaderBeforeData(line, headerName))
				line = reader.ReadLine();
			return IsHeaderBeforeData(line, headerName) ? line : String.Empty;
		}

		private static decimal? ToDecimal(string body)
		{
			if (String.IsNullOrEmpty(body))
				return null;
			decimal value;
			if (decimal.TryParse(body, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
				return value;
			if (decimal.TryParse(body, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
				return value;
			return null;
		}

		public Document ParseIndexingMethod(StreamReader reader, Document document)
		{
			reader.BaseStream.Seek(0, SeekOrigin.Begin);
			reader.DiscardBufferedData();

			var parser = new HeaderBodyParser(reader, CommentSymbol.ToString());
			var line = parser.Header().FirstOrDefault();

            if (line == null)
            {
				throw new Exception("Не найден заголовок накладной");
            }
			
            var header = line.Split(';');
            document.ProviderDocumentId = header[0];
            if (!String.IsNullOrEmpty(header[1]))
                    document.DocumentDate = Convert.ToDateTime(header[1]);

            foreach (var body in parser.Body())
            {

                var parts = body.Split(';');
                var docLine = document.NewLine();
                docLine.Code = parts[0];
                docLine.Product = parts[1];
                docLine.Producer = parts[2];
                docLine.Country = parts[3];
                docLine.Quantity = Convert.ToUInt32(ToDecimal(parts[4]));
                docLine.Certificates = parts[12];
                docLine.SerialNumber = parts[13];
                docLine.RegistryCost = String.IsNullOrEmpty(parts[18]) ? null : ToDecimal(parts[18]);
                docLine.SupplierCost = ToDecimal(parts[5]);
                docLine.SetSupplierCostWithoutNds(ToDecimal(parts[7]).Value);
                docLine.SupplierPriceMarkup = String.IsNullOrEmpty(parts[9]) ? null : ToDecimal(parts[9]);
                docLine.Period = parts[15];
                docLine.ProducerCost = ToDecimal(parts[6]);
                if (parts.Length >= 26 && !String.IsNullOrEmpty(parts[25]) && (ToDecimal(parts[25]) <= 1))
                    docLine.VitallyImportant = (ToDecimal(parts[25]) == 1);
                    //авеста хранит в колонке 11 хранит признак жизненно важный
                else if (parts[10] == "0" || parts[10] == "1")
                    docLine.VitallyImportant = (ToDecimal(parts[10]) == 1);
            }
		    return document;
       }

		public Document Parse(string file, Document document)
		{
			isCorrectBody = true;
			using(var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var line = String.Empty;
				var headerDescription = ReadHeader(reader, HeaderCaption);
				if (headerDescription == null)
					return ParseIndexingMethod(reader, document);

				var header = reader.ReadLine().Split(';');
				document.ProviderDocumentId = headerDescription.GetProviderDocumentId(header);
				document.DocumentDate = headerDescription.GetDocumentDate(header);

				var bodyDescription = ReadHeader(reader, BodyCaption);
				if (!isCorrectBody)
					return null;
				if (bodyDescription == null)
					throw new Exception("Не найдено тело накладной");

				while ((line = reader.ReadLine()) != null)
				{
					if (String.IsNullOrEmpty(line))
						continue;
					var parts = line.Split(';');
					var docLine = document.NewLine();

					docLine.Code = bodyDescription.GetCode(parts);
					docLine.Product = bodyDescription.GetProduct(parts);
					docLine.Producer = bodyDescription.GetProducer(parts);
					docLine.Country = bodyDescription.GetCountry(parts);
					docLine.Quantity = bodyDescription.GetQuantity(parts);
					docLine.Certificates = bodyDescription.GetCertificates(parts);
					docLine.SerialNumber = bodyDescription.GetSerialNumber(parts);
					docLine.RegistryCost = bodyDescription.GetRegistryCost(parts);
					docLine.SupplierCost = bodyDescription.GetSupplierCost(parts);
					docLine.SetSupplierCostWithoutNds(bodyDescription.GetSupplierCostWithoutNds(parts).Value);
					docLine.SupplierPriceMarkup = bodyDescription.GetSupplierPriceMarkup(parts);
					docLine.Period = bodyDescription.GetPeriod(parts);
					docLine.ProducerCost = bodyDescription.GetProducerCost(parts);
					docLine.VitallyImportant = bodyDescription.GetVitallyImportant(parts);
				}
			}
			return document;
		}
	}
}
