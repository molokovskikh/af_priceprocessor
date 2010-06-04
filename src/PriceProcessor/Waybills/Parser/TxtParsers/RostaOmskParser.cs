using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class RostaOmskParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var header = reader.ReadLine();
				var txtParser = new TxtParser(header, '\t');

				while (txtParser.ReadLine(reader))
				{
					document.ProviderDocumentId = txtParser.ProviderDocumentId;
					document.DocumentDate = txtParser.DocumentDate;

					var line = document.NewLine();
					line.Code = txtParser.Code;
					line.Product = txtParser.Product.Trim();
					line.Producer = txtParser.Producer.Trim();
					line.SerialNumber = txtParser.SerialNumber.Trim();
					line.Country = txtParser.Country;
					line.Quantity = txtParser.Quantity;
					line.ProducerCost = txtParser.ProducerCost;
					line.SupplierCost = txtParser.SupplierCost;
					line.SupplierCostWithoutNDS = txtParser.SupplierCostWithoutNds;
					line.Period = txtParser.Period;
					line.RegistryCost = txtParser.RegistryCost;
					line.Certificates = txtParser.Certificates;
					var vi = txtParser.VitallyImportantChar;
					line.VitallyImportant = (vi == null) ? false : vi.Equals("*");
					line.SetValues();
				}
			}
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var headers = reader.ReadLine().Split('\t');
				return (headers[0].ToLower().Equals("фактура") &&
					headers[1].ToLower().Equals("дата") &&
					headers[2].ToLower().Equals("код") &&
					headers[3].ToLower().Equals("наименование товара"));
			}
		}
	}
}
