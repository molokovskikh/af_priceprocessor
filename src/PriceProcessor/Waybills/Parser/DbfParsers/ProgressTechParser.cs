using System.Data;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class ProgressTechParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file);
			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NOM_SHET")
				.DocumentHeader(h => h.DocumentDate, "DATA_SCHET")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "ZAVOD")
				.Line(l => l.Country, "STRANA")
				.Line(l => l.Period, "GOGEN_DO")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.SupplierCost, "CENA_OPT")
				.Line(l => l.SupplierCostWithoutNDS, "CENA_BNDS")
				.Line(l => l.ProducerCost, "CENA_ZAV")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.VitallyImportant, "JV")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.SerialNumber, "SERIA")
				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NOM_SHET")
				&& data.Columns.Contains("DATA_SCHET");
		}
	}
}