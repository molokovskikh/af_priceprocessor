using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class GenezisVrnParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "")
				.DocumentHeader(d => d.ProviderDocumentId, "NUM")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "GOOD")
				.Line(l => l.Producer, "ENTERP")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.SupplierCostWithoutNDS, "PRICEWONDS")
				.Line(l => l.Quantity, "QUANT")
				.Line(l => l.ProducerCost, "PRICEENT")
				.Line(l => l.Period, "DETEB")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.VitallyImportant, "PV")
				.Line(l => l.SerialNumber, "SERIAL");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NUM")
				&& data.Columns.Contains("CODE")
				&& data.Columns.Contains("GOOD")
				&& data.Columns.Contains("SERIAL")
				&& data.Columns.Contains("DETEB");
		}
	}
}
