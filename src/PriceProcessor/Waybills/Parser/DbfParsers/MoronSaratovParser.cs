using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class MoronSaratovParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NUM_NAK")
				.DocumentHeader(d => d.DocumentDate, "DATE_NAK")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NAM_LEK")
				.Line(l => l.Producer, "ZAVOD")
				.Line(l => l.Country, "STRANA")
				.Line(l => l.Period, "SROK_GOD")
				.Line(l => l.SerialNumber, "SERIY")
				.Line(l => l.Quantity, "COUNT")
				.Line(l => l.ProducerCostWithoutNDS, "CENA_ZAVODA")
				.Line(l => l.SupplierCost, "CENA")
				.Line(l => l.Nds, "PRC_NDS")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.SupplierPriceMarkup, "SUMMA_NAC");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NUM_NAK")
				&& data.Columns.Contains("DATE_NAK")
				&& data.Columns.Contains("NAM_LEK")
				&& data.Columns.Contains("STRANA")
				&& data.Columns.Contains("ZAVOD");
		}
	}
}