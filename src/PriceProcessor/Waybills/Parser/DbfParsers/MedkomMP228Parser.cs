using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class MedkomMP228Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NDOC")
				.DocumentHeader(h => h.DocumentDate, "DATEDOC")
				.Invoice(i => i.RecipientId, "PODRCD")
				.Invoice(i => i.Amount, "SUMPAY")
				.Invoice(i => i.InvoiceNumber, "BILLNUM")
				.Invoice(i => i.InvoiceDate, "BILLDT")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE2N")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.Code, "CODEPST")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "SERTDATE")
				.Line(l => l.CertificateAuthority, "SERTORG")
				.Line(l => l.Period, "GDATE")
				.Line(l => l.OrderId, "NUMZ")
				.Line(l => l.BillOfEntryNumber, "NUMGTD")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.ProducerCostWithoutNDS, "PRICEMAN")
				.Line(l => l.Country, "CNTR")
				.Line(l => l.VitallyImportant, "GNVLS")
				.Line(l => l.RegistryCost, "REGPRC")
				.Line(l => l.RegistryDate, "DATEPRC")
				.Line(l => l.Amount, "SUMS0")
				.Line(l => l.NdsAmount, "SUMNDS")
				.Line(l => l.CountryCode, "CNTR_COD");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("CODEPST") &&
				data.Columns.Contains("PRICEMAN") &&
				data.Columns.Contains("PRICE2N") &&
				data.Columns.Contains("GDATE") &&
				data.Columns.Contains("SERTIF") &&
				data.Columns.Contains("QNTPACK") &&
				data.Columns.Contains("CNTR_COD");
		}
	}
}
