using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Parser;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	public class BioFarmVolgaParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NOMERDOK")
				.DocumentHeader(d => d.DocumentDate, "DATADOK")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "TOVAR")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.RegistryCost, "CENAREESTR")
				.Line(l => l.ProducerCost, "CENAIZBNDS")
				.Line(l => l.SupplierCostWithoutNDS, "CENABNDS")
				.Line(l => l.SupplierCost, "CENASNDS")
				.Line(l => l.Nds, "STAVKANDS")
				.Line(l => l.NdsAmount, "NDS")
				.Line(l => l.Amount, "SUMMASNDS")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.Certificates, "SERTIFICAT")
				.Line(l => l.CertificatesDate, "DATASERT")
				.Line(l => l.Producer, "ZAVOD")
				.Line(l => l.Period, "GODENDO")
				.Line(l => l.Country, "STRANA")
				.Line(l => l.BillOfEntryNumber, "GTD");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NOMERDOK")
				&& data.Columns.Contains("DATADOK")
				&& data.Columns.Contains("KOD")
				&& data.Columns.Contains("TOVAR")
				&& data.Columns.Contains("KOLVO")
				&& data.Columns.Contains("CENAIZBNDS")
				&& data.Columns.Contains("CENABNDS")
				&& (data.Columns.Contains("CENASNDS"))
				&& data.Columns.Contains("GODENDO");
		}
	}
}
