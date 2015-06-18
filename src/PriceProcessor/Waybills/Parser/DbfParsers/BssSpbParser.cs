using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class BssSpbParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "NAKLDATA")
				.DocumentHeader(d => d.ProviderDocumentId, "DOK_NAM")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Country, "STRANA")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE_1")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.Quantity, "KOL")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE_M2")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.Period, "GOD_TO")
				.Line(l => l.Certificates, "SERT_VS")
				.Line(l => l.CertificateAuthority, "SERT_FIRM")
				.Line(l => l.CertificatesDate, "SERT_DATE")
				.Line(l => l.RegistryCost, "PRICE_GR")
				.Line(l => l.VitallyImportant, "GVLS")
				.Line(l => l.SerialNumber, "SER");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NAKLDATA")
				&& data.Columns.Contains("DOK_NAM")
				&& data.Columns.Contains("PRICE_M2")
				&& data.Columns.Contains("GOD_TO")
				&& data.Columns.Contains("KOL");
		}
	}
}