using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class KatrenMskKalugaParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "DocDate")
				.DocumentHeader(d => d.ProviderDocumentId, "DocNum")
				.Line(l => l.Code, "GoodId")
				.Line(l => l.Product, "GoodName")
				.Line(l => l.Producer, "Vendor")
				.Line(l => l.Country, "Country")
				.Line(l => l.SupplierCost, "Price")
				.Line(l => l.Quantity, "Amount")
				.Line(l => l.ProducerCostWithoutNDS, "VendorPric")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.Period, "LifeTime")
				.Line(l => l.Certificates, "Sertif")
				.Line(l => l.RegistryCost, "ReestrPr")
				.Line(l => l.VitallyImportant, "VitImport")
				.Line(l => l.SerialNumber, "Series");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("DocNum")
				&& data.Columns.Contains("GoodId")
				&& data.Columns.Contains("GoodName")
				&& data.Columns.Contains("Vendor")
				&& data.Columns.Contains("Amount");
		}
	}
}