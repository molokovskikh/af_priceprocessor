using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class ProfitmedMoscowParser: BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "DDATE")
				.DocumentHeader(d => d.ProviderDocumentId, "REGNAKL")
				.Line(l => l.Code, "CODEGOOD")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.SupplierCostWithoutNDS, "OPTTPWONDS")
				.Line(l => l.SupplierCost, "OPTTPWNDS")
				.Line(l => l.Quantity, "KOL")
				.Line(l => l.ProducerCostWithoutNDS, "POSTPRICE")
				.Line(l => l.Period, "DATEE")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.Certificates, "SERTNAME")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.RegistryCost, "REGPRICE")
				.Line(l => l.SerialNumber, "INV");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("ID")
				&& data.Columns.Contains("TYPE")
				&& data.Columns.Contains("DDATE")
				&& data.Columns.Contains("HDRTITLE")
				&& data.Columns.Contains("N");
		}
	}
}
