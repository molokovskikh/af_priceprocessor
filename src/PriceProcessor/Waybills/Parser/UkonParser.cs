﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class UkonParser : IDocumentParser
	{
		// Символ, который обозначает комментарий
		private const char CommentSymbol = '-';

		private const string HeaderCaption = "[HEADER]";

		private const string BodyCaption = "[BODY]";

		private const string StartLineForHeaders = "В следующей строке перечислены";

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
				return null;
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

		public Document ParseIndexingMethod(StreamReader reader, Document document)
		{
			reader.BaseStream.Seek(0, SeekOrigin.Begin);
			reader.DiscardBufferedData();
			var line = ReadSimpleHeader(reader, HeaderCaption);
			if (String.IsNullOrEmpty(line))
				throw new Exception("Не найден заголовок накладной (чтение методом индексации).");

			var header = reader.ReadLine().Split(';');
			document.ProviderDocumentId = header[0];
			if (!String.IsNullOrEmpty(header[1]))
				document.DocumentDate = Convert.ToDateTime(header[1]);

			line = reader.ReadLine();
			while (IsCommentLine(line) && !IsBodyCaption(line))
				line = reader.ReadLine();

			if (String.IsNullOrEmpty(line))
				throw new Exception("Не найдено тело накладной (чтение методом индексации).");

			while ((line = reader.ReadLine()) != null)
			{
				var parts = line.Split(';');
				var docLine = document.NewLine();
				docLine.Code = parts[0];
				docLine.Product = parts[1];
				docLine.Producer = parts[2];
				docLine.Country = parts[3];
				docLine.Quantity = Convert.ToUInt32(parts[4]);
				docLine.Certificates = parts[12];
				docLine.SerialNumber = parts[13];
				docLine.RegistryCost = String.IsNullOrEmpty(parts[18]) ? null : (decimal?) Convert.ToDecimal(parts[18], CultureInfo.InvariantCulture);
				docLine.SupplierCost = Convert.ToDecimal(parts[5], CultureInfo.InvariantCulture);
				docLine.SetSupplierCostWithoutNds(Convert.ToDecimal(parts[7], CultureInfo.InvariantCulture));
				docLine.SupplierPriceMarkup = String.IsNullOrEmpty(parts[9]) ? null : (decimal?) Convert.ToDecimal(parts[9], CultureInfo.InvariantCulture);
				docLine.Period = parts[15];
				docLine.ProducerCost = Convert.ToDecimal(parts[6], CultureInfo.InvariantCulture);
				if (parts.Length >= 26 && !String.IsNullOrEmpty(parts[25]))
					docLine.VitallyImportant = Convert.ToBoolean(Convert.ToUInt32(parts[25]));
			}
			return document;
		}

		public Document Parse(string file, Document document)
		{
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
