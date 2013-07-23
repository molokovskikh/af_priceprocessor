using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser.Helpers;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class SiaSamaraParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "DCODE")
				.DocumentHeader(d => d.DocumentDate, "DATE_DOC")
				.Line(l => l.Code, "CODE")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.SupplierCost, "PRICE_OPL")
				.Line(l => l.Nds, "NDS_PR")
				.Line(l => l.NdsAmount, "NDS_SUM")
				.Line(l => l.Amount, "SUM_OPL")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE_BASE")
				.Line(l => l.ProducerCost, "PRO")
				.Line(l => l.ProducerCostWithoutNDS, "PRO_NNDS")
				.Line(l => l.SupplierPriceMarkup, "NC_OPT_PR")
				.Line(l => l.RegistryCost, "PRICE_REES")
				.Line(l => l.RegistryDate, "DATE_REES")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.Certificates, "SERT_N")
				.Line(l => l.Product, "PRODUCT")
				.Line(l => l.Producer, "PRODUCER")
				.Line(l => l.Period, "SROK_S")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.VitallyImportant, "ZVLS")
				.Line(l => l.CertificateAuthority, "SERT_N_REG");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("CODE")
				&& data.Columns.Contains("KOLVO")
				&& data.Columns.Contains("PRICE_OPL")
				&& data.Columns.Contains("PRO_NNDS")
				&& !data.Columns.Contains("CNTR")
				&& data.Columns.Contains("ZVLS");
		}
	}
}
