using System.Data;
using Common.Tools;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	/*Сейчас Роста воронеж парсится через AptekaJoldingSingleParser, этот включить 
	 * если в аптеке холдинг что-то поменяется и роста перестанет парсится.
	 * 
	 * class RostaVoronezhParser : IDocumentParser
	{
		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file);
			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "DOCNO")
				.DocumentHeader(h => h.DocumentDate, "DOCDAT")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "TOVAR")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.Quantity, "KOL")
				.Line(l => l.SupplierCostWithoutNDS, "TZENA")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.Country, "STRANA")
				.Line(l => l.Period, "GODEN")
				.Line(l => l.RegistryCost, "REG")
				.Line(l => l.ProducerCost, "ZAVOD")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.SupplierCost, "TZENANDS")
				.Line(l => l.Nds, "NDSSTAVK")
				.Line(l => l.VitallyImportant, "PV")
				.ToDocument(document, data);
			return document;
		}

		public static bool CheckFileFormat(DataTable table)
		{
			return table.Columns.Contains("PV")
				&& table.Columns.Contains("SERTOT")
				&& table.Columns.Contains("ZAVODSNDS")
				&& table.Columns.Contains("GODEN")
				&& table.Columns.Contains("TORGNADB")
				&& table.Columns.Contains("SERTIF")
				&& table.Columns.Contains("EAN13");
		}
	}*/
}
