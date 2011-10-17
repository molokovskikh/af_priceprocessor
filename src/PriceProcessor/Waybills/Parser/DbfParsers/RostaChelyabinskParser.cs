using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class RostaChelyabinskParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "DOCNUMBER")
				.DocumentHeader(h => h.DocumentDate, "DOCDATE")

				.Line(l => l.Code, "ARTICULID")
				.Line(l => l.Product, "ARTICUL")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.Country, "STRANA")
				.Line(l => l.Quantity, "QUANTITY")
				.Line(l => l.VitallyImportant, "ZNVLS")
				.Line(l => l.Nds, "TAX")				
				.Line(l => l.RegistryCost, "PREDELPRIC")
				.Line(l => l.ProducerCostWithoutNDS, "ZAVODNONDS")
				.Line(l => l.SupplierCostWithoutNDS, "PRICENONDS")
				.Line(l => l.SupplierCost, "PRICESNDS")
				.Line(l => l.SupplierPriceMarkup, "NADPROC")
				.Line(l => l.NdsAmount, "SUMMANDS")
				.Line(l => l.Amount, "SUMMASNDS")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.Period, "CERTABATE")
				.Line(l => l.Certificates, "ADQNAME");
		}

		public static bool CheckFileFormat(DataTable table)
		{
			return table.Columns.Contains("DOCNUMBER")
				&& table.Columns.Contains("DOCDATE")
				&& table.Columns.Contains("ARTICULID")
				&& table.Columns.Contains("ARTICUL")
				&& table.Columns.Contains("TAX")
				&& table.Columns.Contains("ZAVODNONDS")
				&& table.Columns.Contains("PRICENONDS");
		}
	}
}
