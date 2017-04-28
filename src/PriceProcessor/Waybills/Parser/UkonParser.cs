﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser.TxtParsers;

namespace Inforoom.PriceProcessor.Waybills.Parser.SstParsers
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

		private static SstDocumentHeader ReadHeader(StreamReader reader, string headerName)
		{
			var line = reader.ReadLine();
			while ((line != null) && IsCommentLine(line) && !IsStartLineForHeaders(line))
				line = reader.ReadLine();
			if (!IsStartLineForHeaders(line)) {
				isCorrectBody = false;
				return null;
			}
			var headerParts = new List<string>();
			while ((line != null) && !IsHeaderBeforeData(line, headerName)) {
				line = reader.ReadLine();
				if (line == null)
					return null;
				headerParts.AddRange(line.Split(';'));
			}
			if (headerParts.Count == 0)
				return null;
			for (var i = 0; i < headerParts.Count; i++) {
				if (String.IsNullOrEmpty(headerParts[i])) {
					headerParts.RemoveAt(i--);
					continue;
				}
				headerParts[i] = headerParts[i].Trim('-', ' ');
			}
			return new SstDocumentHeader(headerParts.ToArray()) { UseSequencedIndexing = true };
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

			if (line == null) {
				throw new Exception("Не найден заголовок накладной");
			}

			var header = line.Split(';');
			document.ProviderDocumentId = header[0];
			if (!String.IsNullOrEmpty(header[1]))
				document.DocumentDate = Convert.ToDateTime(header[1]);

			foreach (var body in parser.Body()) {
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
				var supplierCos = ToDecimal(parts[7]);
				if (supplierCos != null)
					docLine.SetSupplierCostWithoutNds(supplierCos.Value);
				docLine.SupplierPriceMarkup = String.IsNullOrEmpty(parts[9]) ? null : ToDecimal(parts[9]);
				docLine.Period = parts[15];
				docLine.ProducerCostWithoutNDS = ToDecimal(parts[6]);
				if (parts.Length >= 26 && !String.IsNullOrEmpty(parts[25]) && (ToDecimal(parts[25]) <= 1))
					docLine.VitallyImportant = (ToDecimal(parts[25]) == 1);
				//авеста хранит в колонке 11 хранит признак жизненно важный
				else if (parts[10] == "0" || parts[10] == "1")
					docLine.VitallyImportant = (ToDecimal(parts[10]) == 1);
				// http://redmine.analit.net/issues/60333
				else if (parts[19] == "0" || parts[19] == "1")
					docLine.VitallyImportant = (ToDecimal(parts[19]) == 1);
				if (parts[16].Length == 13)
					docLine.EAN13 = NullableConvert.ToUInt64(parts[16]);
			}
			return document;
		}

		public Document Parse(string file, Document document)
		{
			isCorrectBody = true;
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251))) {
				var line = String.Empty;
				var headerDescription = ReadHeader(reader, HeaderCaption);
				if (headerDescription == null)
					return ParseIndexingMethod(reader, document);

				var header = reader.ReadLine().Split(';');
				document.ProviderDocumentId = headerDescription.GetProviderDocumentId(header);
				document.DocumentDate = headerDescription.GetDocumentDate(header);

				document.SetInvoice().Amount = headerDescription.GetAmount(header);
				document.SetInvoice().NDSAmount10 = headerDescription.GetNDSAmount10(header);
				document.SetInvoice().NDSAmount18 = headerDescription.GetNDSAmount18(header);
				document.SetInvoice().RecipientId = headerDescription.GetRecipientId(header);

				var bodyDescription = ReadHeader(reader, BodyCaption);
				if (!isCorrectBody)
					return null;
				if (bodyDescription == null)
					throw new Exception("Не найдено тело накладной");

				while ((line = reader.ReadLine()) != null) {
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
					docLine.RegistryDate = bodyDescription.GetRegistryDate(parts);
					docLine.SupplierCost = bodyDescription.GetSupplierCost(parts);
					if (bodyDescription.GetSupplierCostWithoutNds(parts) != null)
						docLine.SetSupplierCostWithoutNds(bodyDescription.GetSupplierCostWithoutNds(parts).Value);
					docLine.SupplierPriceMarkup = bodyDescription.GetSupplierPriceMarkup(parts);
					docLine.Period = bodyDescription.GetPeriod(parts);
					docLine.ProducerCostWithoutNDS = bodyDescription.GetProducerCost(parts);
					docLine.VitallyImportant = bodyDescription.GetVitallyImportant(parts);
					docLine.EAN13 = NullableConvert.ToUInt64(bodyDescription.GetEAN13(parts));
					docLine.BillOfEntryNumber = bodyDescription.GetBillOfEntryNumber(parts);
					docLine.DateOfManufacture = bodyDescription.GetDateOfManufacture(parts);
					docLine.ExpireInMonths = bodyDescription.GetExpireInMonths(parts);
				}
			}
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251))) {
				var parser = new HeaderBodyParser(reader, CommentSymbol.ToString());
				var headerLine = parser.Header().FirstOrDefault();
				if (headerLine == null)
					return false;
				var header = headerLine.Split(';');
				DateTime dt;
				if (!DateTime.TryParse(header[1], out dt))
					return false;
				if (header.Length != 9 && header.Length != 18 && header.Length != 11 && header.Length != 10 && header.Length != 17 && header.Length != 6)
					return false;
				var bodyLine = parser.Body().FirstOrDefault();
				if (bodyLine == null)
					return false;
				var body = bodyLine.Split(';');
				if (body.Length != 22 && body.Length != 24 && body.Length != 27 && body.Length != 26 && body.Length != 21 && body.Length != 31 && body.Length != 20 && body.Length != 32 && body.Length != 25)
					return false;
				// http://redmine.analit.net/issues/53074
				if (header.Length == 9 && body.Length == 22) {
					var s = "";
					for (int i = 3; i < 9; i++)
						s += header[i];
					if (s.Length == 0)
						return false;
				}
			}
			return true;
		}
	}
}