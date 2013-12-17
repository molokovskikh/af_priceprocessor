using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class KatrenOrelParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NDOC")
				.DocumentHeader(h => h.DocumentDate, "DATEDOC")
				.Invoice(i => i.Amount, "SUMPAY")
				.Invoice(i => i.NDSAmount, "SUMNDS10")
				.Invoice(i => i.AmountWithoutNDS, "SUM0")
				.Invoice(i => i.RecipientId, "Destid")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.ProducerCostWithoutNDS, "PRICE1")
				.Line(l => l.SupplierCost, "PRICE2")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE2N")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.Period, "GDATE")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Country, "CNTR")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.RegistryCost, "REGPRC")
				.Line(l => l.RegistryDate, "DATEPRC")
				.Line(l => l.BillOfEntryNumber, "NUMGTD")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "SERTDATE")
				.Line(l => l.CertificateAuthority, "SERTORG");
		}
		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("NDOC")
				&& data.Columns.Contains("CNTR")
				&& data.Columns.Contains("GDATE")
				&& data.Columns.Contains("PRICE2N")
				&& data.Columns.Contains("Destid")
				&& data.Columns.Contains("GVId");
		}
	}
}
