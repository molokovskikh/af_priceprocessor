using System.IO;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.PdParsers
{
	public class ProtekParser : IDocumentParser
	{
		public static bool CheckFileFormat(string file)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251))) {
				var version = reader.ReadLine();
				if (version != "V2")
					return false;
				for (int i = 0; i < 4; i++)
					if (reader.ReadLine() == null)
						return false;
			}
			return true;
		}

		public Document Parse(string file, Document document)
		{
			using (var reader = new StreamReader(file, Encoding.GetEncoding(1251))) {
				reader.ReadLine();
				var separator = reader.ReadLine()[0];

				var headerCaptions = reader.ReadLine().Split(separator);
				var headerParts = reader.ReadLine().Split(separator);

				var bodyHeader = reader.ReadLine();
				var header = new ProtekDocumentHeader(bodyHeader, separator);

				document.DocumentDate = header.GetDocumentDate(headerCaptions, headerParts);
				document.ProviderDocumentId = header.GetProviderDocumentId(headerCaptions, headerParts);
				string line;
				while ((line = reader.ReadLine()) != null) {
					var parts = line.Split(separator);
					var docLine = new DocumentLine {
						Code = header.GetCode(parts),
						Product = header.GetProduct(parts),
						Producer = header.GetProducer(parts),
						Country = header.GetCountry(parts),
						Quantity = header.GetQuantity(parts),
						SupplierCost = header.GetSupplierCost(parts),
						SupplierCostWithoutNDS = header.GetSupplierCostWithoutNds(parts),
						Certificates = header.GetCertificates(parts),
						ProducerCostWithoutNDS = header.GetProducerCost(parts),
						RegistryCost = header.GetRegistryCost(parts),
						Nds = header.GetNds(parts),
						Period = header.GetPeriod(parts),
						VitallyImportant = header.GetVitallyImportant(parts),
						SerialNumber = header.GetSerialNumber(parts),
					};
					document.NewLine(docLine);
				}
			}
			return document;
		}
	}
}