using System.IO;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.TxtParsers
{
	public class KatrenOmskParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var header = reader.ReadLine();
				var txtParser = new TxtParser(header, '|');
				reader.ReadLine();
				while (txtParser.ReadLine(reader))
				{
					document.ProviderDocumentId = txtParser.ProviderDocumentId;
					document.DocumentDate = txtParser.DocumentDate;

					var line = document.NewLine();
					line.Code = txtParser.Code;
					line.Product = txtParser.Product;
					line.Producer = txtParser.Producer;
					line.SerialNumber = txtParser.SerialNumber;
					line.Country = txtParser.Country;
					line.Quantity = txtParser.Quantity;
					line.ProducerCost = txtParser.ProducerCost;
					line.SupplierCost = txtParser.SupplierCost;
					line.Nds = txtParser.Nds;
					line.SupplierCostWithoutNDS = txtParser.SupplierCostWithoutNds;
					line.Period = txtParser.Period;
					line.RegistryCost = txtParser.RegistryCost;
					line.Certificates = txtParser.Certificates;
					var vi = txtParser.VitallyImportantChar;
					line.VitallyImportant = (vi == null) ? false : vi.Equals("*");
				}
			}
			return document;
		}

		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
			{
				var headers = reader.ReadLine().Split('|');
				if (headers.Length < 7)
					return false;
				return (headers[3].ToLower().Equals("дата фактуры") &&
					headers[6].ToLower().Equals("кол-во") &&
					headers[5].ToLower().Equals("ед.изм.") &&
					headers[4].ToLower().Equals("наименование препарата"));
			}
		}
	}
}
