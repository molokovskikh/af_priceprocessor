using System;
using System.IO;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class AmarilisParser : IDocumentParser
	{
		public static bool CheckFileFormat(string file)
		{
			if (!Path.GetExtension(file).Match(".txt"))
				return false;

			using (var reader = new StreamReader(file, Encoding.UTF8)) {
				var line = reader.ReadLine();
				if (String.IsNullOrEmpty(line))
					return false;
				if (line.Split('|').Length != 16)
					return false;
				return true;
			}
		}

		public Document Parse(string file, Document document)
		{
			using (var reader = new StreamReader(file, Encoding.UTF8)) {
				string line;
				while ((line = reader.ReadLine()) != null) {
					var parts = line.Split('|');
					if (parts.Length < 16)
						continue;
					if (String.IsNullOrEmpty(document.ProviderDocumentId)) {
						document.ProviderDocumentId = parts[3];
						document.DocumentDate = NullableConvert.ToDateTime(parts[4]);
					}
					var docLine = document.NewLine();
					docLine.Code = parts[5];
					docLine.Product = parts[6];
					docLine.Quantity = NullableConvert.ToUInt32(parts[7]);
					docLine.SupplierCost = NullableConvert.ToDecimal(parts[8]);
					docLine.SupplierCostWithoutNDS = NullableConvert.ToDecimal(parts[9]);
					docLine.Nds = NullableConvert.ToUInt32(parts[10]);
					docLine.NdsAmount = NullableConvert.ToDecimal(parts[11]);
					docLine.Amount = NullableConvert.ToDecimal(parts[12]);
					docLine.Certificates = parts[13];
					docLine.Producer = parts[14];
				}
			}
			return document;
		}
	}
}