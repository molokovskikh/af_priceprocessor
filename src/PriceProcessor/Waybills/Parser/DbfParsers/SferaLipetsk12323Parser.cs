using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class SferaLipetsk12323Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.Line(l => l.Code, "CODE")
				.Line(l => l.Product, "GOOD")
				.Line(l => l.SerialNumber, "SERIAL")
				.Line(l => l.Period, "DETEB")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.Quantity, "QUANT")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.CertificatesDate, "DATES")
				.Line(l => l.CertificateAuthority, "SERTWHO")
				.Line(l => l.SupplierPriceMarkup, "MARGIN")
				.Line(l => l.NdsAmount, "NDS")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.Producer, "ENTERP")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.SupplierCostWithoutNDS, "PRICEWONDS")
				.Line(l => l.EAN13, "EAN")
				.Line(l => l.UnitCode, "PCE")
				.Line(l => l.CountryCode, "CNTRCODE")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.ProducerCost, "PRICEENT");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("GOOD")
				&& data.Columns.Contains("DETEB")
				&& data.Columns.Contains("SERT")
				&& data.Columns.Contains("SERTWHO")
				&& !data.Columns.Contains("PV");
		}
	}
}