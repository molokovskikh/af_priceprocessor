using System.Data;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class FarmaimpeksIzhevskParser : IDocumentParser
	{
		protected Encoding Encoding = Encoding.GetEncoding(866);

		public Document Parse(string file, Document document)
		{
			var data = Dbf.Load(file, Encoding);

			new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NDOCNUM")
				.DocumentHeader(h => h.DocumentDate, "NDATE")
				.Line(l => l.Code, "ACWARES")
				.Line(l => l.Product, "WARESNAME")
				.Line(l => l.Producer, "PRODNAME")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.ProducerCostWithoutNDS, "PLT_NO_NDS")
				.Line(l => l.SupplierCost, "ACSELLCOST")
				.Line(l => l.SupplierCostWithoutNDS, "SELL_NO_ND")
				.Line(l => l.Quantity, "ACAMOUNT")
				.Line(l => l.Period, "ACVALDATE")
				.Line(l => l.RegistryCost, "RECOST")
				.Line(l => l.Certificates, "CERTNUM")
				.Line(l => l.SerialNumber, "ACSERIES")
				.Line(l => l.VitallyImportant, "IS_VITAL")
				.Line(l => l.Nds, "NDS")
				.ToDocument(document, data);

			return document;
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("ACWARES") &&
				data.Columns.Contains("WARESNAME") &&
				data.Columns.Contains("ACAMOUNT") &&
				data.Columns.Contains("PLT_NO_NDS") &&
				data.Columns.Contains("NDOCNUM") &&
				data.Columns.Contains("COUNTRY");
		}
	}
}