using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class NadezhdaFarmTambovOrelParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "ORDERNUM")
				.DocumentHeader(h => h.DocumentDate, "ORDERDATE")
				.Line(l => l.Product, "NAMEGOOD")
				.Line(l => l.Code, "IDGOOD")
				.Line(l => l.SupplierCost, "PRICEOUT")
				.Line(l => l.SupplierCostWithoutNDS, "PRICEOPT")
				.Line(l => l.ProducerCostWithoutNDS, "PRPROD")
				.Line(l => l.SupplierPriceMarkup, "PROFIT")
				.Line(l => l.Nds, "PROCNDS")
				.Line(l => l.Quantity, "QUANTIS")
				.Line(l => l.Amount, "SUMOUT")
				.Line(l => l.NdsAmount, "SUMNDS")
				.Line(l => l.Producer, "FIRMNAME")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.Period, "DATEBEST")
				.Line(l => l.VitallyImportant, "JNVLS")
				.Line(l => l.RegistryCost, "REESTR");
		}

		public static bool CheckFileFormat(DataTable table)
		{
			return table.Columns.Contains("ORDERNUM") &&
				table.Columns.Contains("NAMEGOOD") &&
				table.Columns.Contains("IDGOOD") &&
				table.Columns.Contains("PRICEOUT") &&
				table.Columns.Contains("PRICEOPT");
		}
	}
}