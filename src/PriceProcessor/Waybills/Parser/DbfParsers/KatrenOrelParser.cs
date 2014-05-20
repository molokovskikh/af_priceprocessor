using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class KatrenOrelParser2 : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "DATE_DOK")
				.DocumentHeader(d => d.ProviderDocumentId, "NUM_DOC")
				.Line(l => l.Code, "CODE_TOVAR")
				.Line(l => l.CodeCr, "VCODE")
				.Line(l => l.Product, "NAME_TOVAR")
				.Line(l => l.Producer, "PROIZ")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE")
				.Line(l => l.Quantity, "VOLUME")
				.Line(l => l.ProducerCostWithoutNDS, "PR_PRICE")
				.Line(l => l.Period, "SROK")
				.Line(l => l.Certificates, "DOKUMENT")
				.Line(l => l.CertificateAuthority, "SERTWHO")
				.Line(l => l.Nds, "PCT_NDS")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.VitallyImportant, "JNVLS")
				.Line(l => l.RegistryCost, "REESTR")
				.Line(l => l.EAN13, "BARCOD")
				.Line(l => l.BillOfEntryNumber, "GTD");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("DATE_DOK")
				&& data.Columns.Contains("NUM_DOC")
				&& data.Columns.Contains("CODE_TOVAR")
				&& data.Columns.Contains("VOLUME")
				&& data.Columns.Contains("SROK")
				&& data.Columns.Contains("PR_PRICE")
				&& data.Columns.Contains("DOKUMENT")
				&& data.Columns.Contains("BARCOD");
		}
	}

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
