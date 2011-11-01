using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class MedicineSpbDbfParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "DDATE")
				.DocumentHeader(d => d.ProviderDocumentId, "DNUM")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "PRRNAME")
				.Line(l => l.Country, "CNTRNAME")
				.Line(l => l.SupplierCostWithoutNDS, "PRICEEV")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.Quantity, "QTY")
				.Line(l => l.ProducerCostWithoutNDS, "PRRPRICE")
				.Line(l => l.Period, "BESTB")
				.Line(l => l.Nds, "VATV")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.VitallyImportant, "VITAL")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.RegistryCost, "PriceGR");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("DDATE")
				&& data.Columns.Contains("DNUM")
				&& data.Columns.Contains("PRRNAME")
				&& data.Columns.Contains("BESTB")
				&& data.Columns.Contains("QTY");
		}
	}
}
