using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser
{
	public class UkonParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			using(var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				reader.ReadLine();
				reader.ReadLine();
				reader.ReadLine();

				reader.ReadLine();
				reader.ReadLine();
				reader.ReadLine();
				reader.ReadLine();
				reader.ReadLine();
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					var parts = line.Split(';');
					var docLine = document.NewLine();
					docLine.Code = parts[0];
					docLine.Product = parts[1];
					docLine.Producer = parts[2];
					docLine.Country = parts[3];
					docLine.Quantity = Convert.ToUInt32(parts[4]);
					docLine.SupplierCost = Convert.ToDecimal(parts[5], CultureInfo.InvariantCulture);
					docLine.SupplierCostWithoutNDS = Convert.ToDecimal(parts[7], CultureInfo.InvariantCulture);
					docLine.SupplierPriceMarkup = Convert.ToDecimal(parts[9], CultureInfo.InvariantCulture);
					docLine.ProducerCost = Convert.ToDecimal(parts[6], CultureInfo.InvariantCulture);
				}
			}
			return document;
		}
	}
}
