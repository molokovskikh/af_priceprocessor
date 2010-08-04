using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class AvestaFarmatsevtikaParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "DATE_DOK")
				.DocumentHeader(d => d.ProviderDocumentId, "NUM_DOC")
				.Line(l => l.Code, "CODE_TOVAR")
				.Line(l => l.Product, "NAME_TOVAR")
				.Line(l => l.Producer, "PROIZ")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE")
				.Line(l => l.Quantity, "VOLUME")
				.Line(l => l.ProducerCost, "PR_PRICE")
				.Line(l => l.Period, "SROK")
				.Line(l => l.Certificates, "DOKUMENT")
				.Line(l => l.Nds, "PCT_NDS")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.VitallyImportant, "JNVLS")
				.Line(l => l.RegistryCost, "REESTR");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("DATE_DOK")
				&& data.Columns.Contains("NUM_DOC")
				&& data.Columns.Contains("CODE_TOVAR")
				&& data.Columns.Contains("VOLUME")
				&& data.Columns.Contains("SROK")
				&& data.Columns.Contains("PR_PRICE")
				&& data.Columns.Contains("DOKUMENT");
		}
	}
}
