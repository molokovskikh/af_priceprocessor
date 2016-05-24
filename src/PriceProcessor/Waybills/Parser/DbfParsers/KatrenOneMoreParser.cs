using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class KatrenOneMoreParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.DocumentDate, "ITEMDATE")
				.DocumentHeader(d => d.ProviderDocumentId, "DOCNUM")
				.Line(l => l.Amount, "SUMWITHNDS")
				.Line(l => l.BillOfEntryNumber, "GTD")
				.Line(l => l.Certificates, "SERTNAME")
				.Line(l => l.CertificatesDate, "SERTDATEB")
				.Line(l => l.CertificatesEndDate, "SERTDATEF")
				.Line(l => l.CertificateAuthority, "SERTCENTER")
				.Line(l => l.Code, "GOODCODE")
				.Line(l => l.Country, "COUNTRY")
				.Line(l => l.EAN13, "BARFACT")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.NdsAmount, "SUMNDS")
				.Line(l => l.OrderId, "QUERYNUM")
				.Line(l => l.Period, "LIFETIME")
				.Line(l => l.CodeCr, "VENDORCODE")
				.Line(l => l.Producer, "VENDOR")
				.Line(l => l.ProducerCost, "VENDPWITHN")
				.Line(l => l.ProducerCostWithoutNDS, "VENDP")
				.Line(l => l.Product, "ITEMNAME")
				.Line(l => l.Quantity, "AMOUNT")
				.Line(l => l.RegistryCost, "REGPRICE")
				.Line(l => l.RegistryDate, "REGDATE")
				.Line(l => l.SerialNumber, "SERIES")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.SupplierCostWithoutNDS, "PRICENONDS")
				.Line(l => l.VitallyImportant, "VITIMPORT");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("EXTRAP")
				&& data.Columns.Contains("EXTRAR")
				&& data.Columns.Contains("VENDPWITHN")
				&& data.Columns.Contains("PACKAGE");
		}
	}
}
