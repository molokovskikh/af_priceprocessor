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
					docLine.Quantity = Convert.ToUInt32(line[4]);
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
						docLine.VitallyImportant = Convert.ToBoolean(Convert.ToUInt32(line[18]));
				}
			}
			return document;
		}

		private decimal? GetDecimal(string value)
		{
			decimal res;
			if (decimal.TryParse(value, out res))
				return res;
			if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out res))
				return res;
			throw new Exception(String.Format("Не удалось преобразовать значение {0} к типу decimal", value));
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
			}
			return true;
		}
	}
}