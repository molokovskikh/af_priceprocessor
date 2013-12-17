using System;
using System.Data;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class PulseKazan13149Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NUM_NAKL")
				.DocumentHeader(d => d.DocumentDate, "DATE_NAKL")
				.Invoice(i => i.BuyerName, "CLIENT")
				.Line(l => l.Code, "CODE_TOVAR")
				.Line(l => l.Product, "NAME_TOVAR")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.VitallyImportant, "ISGLV")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.SupplierCostWithoutNDS, "CENA")
				.Line(l => l.SupplierCost, "CENANDS")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.NdsAmount, "SUMMA_NDS")
				.Line(l => l.Amount, "SUMMA")
				.Line(l => l.RegistryCost, "CENAREESTR")
				.Line(l => l.ProducerCostWithoutNDS, "CENAPROIZ")
				.Line(l => l.SupplierPriceMarkup, "NACENKA")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.Period, "SROKGODN")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "SERTIF_DAT")
				.Line(l => l.CertificateAuthority, "SERTIF_ORG")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.EAN13, "BARCODE");
		}

		public static bool CheckFileFormat(DataTable table)
		{
			return table.Columns.Contains("CODE_TOVAR")
				&& table.Columns.Contains("NAME_TOVAR")
				&& table.Columns.Contains("ISGLV")
				&& table.Columns.Contains("SROKGODN");
		}
	}
}