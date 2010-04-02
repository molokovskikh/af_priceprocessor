using System;
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

		private static string ReadHeader(StreamReader reader)
		{
			var line = reader.ReadLine();
			while (IsCommentLine(line) && !IsHeaderCaption(line))
				line = reader.ReadLine();
			return IsHeaderCaption(line) ? line : String.Empty;
		}

		public Document Parse(string file, Document document)
		{
			using(var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var line = ReadHeader(reader);
				if (String.IsNullOrEmpty(line))
					throw new Exception(String.Format("Не найден заголовок накладной. Файл {0}", file));

				var header = reader.ReadLine().Split(';');
				document.ProviderDocumentId = header[0];
				if (!String.IsNullOrEmpty(header[1]))
                    document.DocumentDate = Convert.ToDateTime(header[1]);

				line = reader.ReadLine();
				while (IsCommentLine(line) && !IsBodyCaption(line))
					line = reader.ReadLine();

				if (String.IsNullOrEmpty(line))
					throw new Exception(String.Format("Не найдено тело накладной. Файл {0}", file));

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
					docLine.SupplierCost = Convert.ToDecimal(parts[5], CultureInfo.InvariantCulture);
					docLine.SetProducerCostWithoutNds(Convert.ToDecimal(parts[7], CultureInfo.InvariantCulture));
					docLine.SupplierPriceMarkup = Convert.ToDecimal(parts[9], CultureInfo.InvariantCulture);					
					docLine.Period = parts[15];
					docLine.ProducerCost = Convert.ToDecimal(parts[6], CultureInfo.InvariantCulture);
				}
			}
			return document;
		}
	}
}
