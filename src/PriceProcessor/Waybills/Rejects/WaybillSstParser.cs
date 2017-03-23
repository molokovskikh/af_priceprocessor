using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Castle.Components.DictionaryAdapter;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser;
using log4net;
using LumiSoft.Net.Log;
using NHibernate.Mapping;

namespace Inforoom.PriceProcessor.Waybills.Rejects
{
	public class WaybillSstParser
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
			if (line == null) {
				return null;
			}
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
			return new SstDocumentHeader(headerParts.ToArray()) { UseSequencedIndexing = true };
		}

		public Document Parse(string file, Document document)
		{
			isCorrectBody = true;
			try {
				using (var reader = new StreamReader(file, Encoding.GetEncoding(1251))) {
					var line = String.Empty;
					var headerDescription = ReadHeader(reader, HeaderCaption);
					if (headerDescription == null)
						return null;

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
						docLine.EAN13 = bodyDescription.GetEAN13(parts);
						docLine.BillOfEntryNumber = bodyDescription.GetBillOfEntryNumber(parts);
						docLine.DateOfManufacture = bodyDescription.GetDateOfManufacture(parts);
						docLine.ExpireInMonths = bodyDescription.GetExpireInMonths(parts);
					}
				}
			} catch (Exception e) {
				throw new Exception(
					$"У sst парсера {nameof(WaybillSstParser)} при разборе документа '{file}' в методе Parse возникла ошибка", e);
			}
			return document;
		}
	}
}