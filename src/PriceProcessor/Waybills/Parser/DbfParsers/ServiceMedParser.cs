using System;
using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class ServiceMedParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "DATA")
				.DocumentHeader(d => d.ProviderDocumentId, "NOMER")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FIRMA")
				.Line(l => l.Country, "WORK")
				.Line(l => l.SupplierCostWithoutNDS, "CENA1")
				.Line(l => l.SupplierCost, "CENA2")
				.Line(l => l.Quantity, "KOL")
				.Line(l => l.Period, "GOD")
				.Line(l => l.Certificates, "NOMSERT")
				.Line(l => l.SerialNumber, "SERKOD");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NOMER")
				&& data.Columns.Contains("FIRMA")
				&& data.Columns.Contains("WORK");
		}
	}
}