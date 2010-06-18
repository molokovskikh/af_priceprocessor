using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class KatrenOrelTxtParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				reader.ReadLine();
				var header = reader.ReadLine().Split(';');
				document.ProviderDocumentId = header[0];
				if (!String.IsNullOrEmpty(header[1]))
					document.DocumentDate = Convert.ToDateTime(header[1]);

				reader.ReadLine();
				var bodyLine = String.Empty;
				while ((bodyLine = reader.ReadLine()) != null)
				{
					if (String.IsNullOrEmpty(bodyLine))
						continue;
					var line = bodyLine.Split(';');
					var docLine = document.NewLine();

					docLine.Code = line[0];
					docLine.Product = line[1];
					docLine.Producer = line[2];
					if (!String.IsNullOrEmpty(line[3]))
						docLine.Country = line[3];
					docLine.Quantity = Convert.ToUInt32(GetDecimal(line[4]));
					if (!String.IsNullOrEmpty(line[5]))
						docLine.ProducerCost = GetDecimal(line[5]);
					docLine.SupplierCost = GetDecimal(line[6]);
					docLine.SetNds(GetDecimal(line[7]).Value);
					if (!String.IsNullOrEmpty(line[8]))
						docLine.SupplierPriceMarkup = GetDecimal(line[8]);
					if (!String.IsNullOrEmpty(line[9]))
						docLine.SerialNumber = line[9];
					if (!String.IsNullOrEmpty(line[10]))
						docLine.Period = line[10];
					if (!String.IsNullOrEmpty(line[12]))
						docLine.Certificates = line[12];
					if (!String.IsNullOrEmpty(line[16]))
						docLine.RegistryCost = GetDecimal(line[16]);
					if (line.Length > 18 && !String.IsNullOrEmpty(line[18]))
						docLine.VitallyImportant = GetBool(line[18]);
				}
			}
			return document;
		}

		private static decimal? GetDecimal(string value)
		{
			decimal res;
			if (decimal.TryParse(value, out res))
				return res;
			if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out res))
				return res;
			return null;
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

		public static bool CheckFileFormat(string file)
		{
			if (Path.GetExtension(file).ToLower() != ".txt")
				return false;
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var headerCaption = reader.ReadLine();
				if (!headerCaption.ToLower().Equals("[header]"))
					return false;
				reader.ReadLine();
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