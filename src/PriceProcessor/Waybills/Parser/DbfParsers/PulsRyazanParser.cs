using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class PulsRyazanParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "")
				.DocumentHeader(d => d.ProviderDocumentId, "")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "GOODE")
				.Line(l => l.Producer, "PRODUCER")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.Quantity, "QUANT")
				.Line(l => l.ProducerCostWithoutNDS, "PPRICEWT")
				.Line(l => l.Period, "DATEB")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.VitallyImportant, "JVLS")
				.Line(l => l.SerialNumber, "SERIAL");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NUM")
				&& data.Columns.Contains("CODE")
				&& data.Columns.Contains("GOODE")
				&& data.Columns.Contains("PRODUCER")
				&& data.Columns.Contains("PPRICEWT")
				&& data.Columns.Contains("SERIAL")
				&& data.Columns.Contains("DATEB");
		}
	}
}
