using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class SiaKazanParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "DATA_NAKL")
				.DocumentHeader(d => d.ProviderDocumentId, "NUM_NAKL")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.SupplierCostWithoutNDS, "CENABNDS")
				.Line(l => l.SupplierCost, "CENASNDS")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.ProducerCost, "CENAPROIZ")
				.Line(l => l.Period, "SROK_GODN")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.SerialNumber, "SERII");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("KOD")
				&& data.Columns.Contains("NAME")
				&& data.Columns.Contains("NUM_NAKL")
				&& data.Columns.Contains("CENASNDS")
				&& data.Columns.Contains("SROK_GODN")
				&& data.Columns.Contains("PROIZV");
		}
	}
}
