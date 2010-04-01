using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class ProtekParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			using(var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var version = reader.ReadLine();
				if (version != "V2")
					throw new Exception(String.Format("Неизвестная версия документа {0}, {1}", version, file));

				var separator = reader.ReadLine()[0];
				reader.ReadLine();
				var header = reader.ReadLine();
				var headerParts = header.Split(separator);
				document.DocumentDate = Convert.ToDateTime(headerParts[2]);
				reader.ReadLine();
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					var parts = line.Split(separator);
					var docLine = new DocumentLine {
						Code = parts[0],
						Product = parts[1],
						Producer = parts[2],
						Country = parts[3],
						Quantity = uint.Parse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture),
						SupplierCost = Convert.ToDecimal(parts[5], CultureInfo.InvariantCulture),
						Certificates = parts[7],
						ProducerCost = Convert.ToDecimal(parts[8], CultureInfo.InvariantCulture),
						RegistryCost = Convert.ToDecimal(parts[11], CultureInfo.InvariantCulture)
					};
					docLine.SetNds(Convert.ToDecimal(parts[9], CultureInfo.InvariantCulture));
					document.NewLine(docLine);
				}
			}

			return document;
		}
	}
}
