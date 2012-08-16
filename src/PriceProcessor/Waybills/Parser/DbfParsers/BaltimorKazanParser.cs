using System.Data;
using System.IO;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class BaltimorKazanParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NUMDOC")
				.DocumentHeader(d => d.DocumentDate, "DATADOC")
				.Line(l => l.Code, "TOVCODE")
				.Line(l => l.Product, "TOVNAME")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.Nds, "STNDS")
				.Line(l => l.ProducerCostWithoutNDS, "CENAPROIZV")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.CertificatesDate, "DATEREG")
				.Line(l => l.Period, "DATE_VAL")
				.Line(l => l.Producer, "FACTORY")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.RegistryCost, "CGR")
				.Line(l => l.Amount, "SUMASNDS")
				.Line(l => l.EAN13, "STRKOD");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NUMDOC")
				&& data.Columns.Contains("DATADOC")
				&& data.Columns.Contains("TOVCODE")
				&& data.Columns.Contains("TOVNAME")
				&& data.Columns.Contains("KOLVO")
				&& data.Columns.Contains("STNDS")
				&& data.Columns.Contains("CENAPROIZV")
				&& data.Columns.Contains("SERIA")
				&& data.Columns.Contains("SERT")
				&& data.Columns.Contains("DATE_VAL")
				&& data.Columns.Contains("FACTORY")
				&& data.Columns.Contains("PRICE")
				&& data.Columns.Contains("GNVLS")
				&& data.Columns.Contains("CGR")
				&& data.Columns.Contains("SUMASNDS")
				&& data.Columns.Contains("STRKOD");
		}
	}
}