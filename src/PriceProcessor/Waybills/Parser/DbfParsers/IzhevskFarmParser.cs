using System.Data;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class IzhevskFarmParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NTTN")
				.DocumentHeader(h => h.DocumentDate, "DTTN")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "TOVAR")
				.Line(l => l.Producer, "IZGOT")
				.Line(l => l.Country, "STRANA")
				.Line(l => l.ProducerCostWithoutNDS, "CENAIZG")
				.Line(l => l.SupplierCostWithoutNDS, "CENABEZNDS")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.Period, "GODENDO")
				.Line(l => l.RegistryCost, "CENAREESTR")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.VitallyImportant, "JNVLS")
				.Line(l => l.Nds, "STAVKANDS")
				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NTTN") &&
				data.Columns.Contains("TOVAR") &&
				data.Columns.Contains("IZGOT") &&
				data.Columns.Contains("CENABEZNDS") &&
				data.Columns.Contains("KOLVO") &&
				data.Columns.Contains("STAVKANDS");
		}
	}
}