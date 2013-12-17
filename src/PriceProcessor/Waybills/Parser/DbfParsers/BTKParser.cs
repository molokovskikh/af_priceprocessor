using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class BTKParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NDOC")
				.DocumentHeader(d => d.DocumentDate, "DATEDOC")
				.Invoice(i => i.RecipientId, "CODEPST")
				.Invoice(i => i.RecipientAddress, "SEDADR")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.Country, "CNTR")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.SupplierCost, "PRICE2")
				.Line(l => l.SupplierCostWithoutNDS, "PRICE1")
				.Line(l => l.BillOfEntryNumber, "NUMGTD")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "SERTDATE")
				.Line(l => l.CertificateAuthority, "SERTORG")
				.Line(l => l.NdsAmount, "SUMNDS")
				.Line(l => l.Amount, "SUMPAY");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("SUMPAY") &&
				data.Columns.Contains("SERTORG") &&
				data.Columns.Contains("PRICE1") &&
				data.Columns.Contains("SEDADR") &&
				data.Columns.Contains("SERTIF") &&
				data.Columns.Contains("DATEDOC");
		}
	}
}
