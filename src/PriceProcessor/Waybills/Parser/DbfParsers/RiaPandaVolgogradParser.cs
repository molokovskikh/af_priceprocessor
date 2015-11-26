using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class RiaPandaVolgogradParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "NDOC")
				.DocumentHeader(h => h.DocumentDate, "DATEDOC")
				.Line(l => l.Product, "NAME")
				.Line(l => l.Code, "CODEPST")
				.Line(l => l.Quantity, "QNT")
				.Line(l => l.SerialNumber, "SER")
				.Line(l => l.Period, "GDATE")
				.Line(l => l.SupplierCost, "PRICE")
				.Line(l => l.SupplierCostWithoutNDS, "PRICEF")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.Producer, "FIRM")
				.Line(l => l.EAN13, "EAN13")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificateSerialNumber, "SERN")
				.Line(l => l.CertificatesDate, "SERTDATE")
				.Line(l => l.CertificateAuthority, "SERTORG");
		}

		public static bool CheckFileFormat(DataTable table)
		{
			return table.Columns.Contains("NDOC")
				&& table.Columns.Contains("DATEDOC")
				&& table.Columns.Contains("NAME")
				&& table.Columns.Contains("CODEPST")
				&& table.Columns.Contains("QNT")
				&& table.Columns.Contains("SER")
				&& table.Columns.Contains("GDATE")
				&& table.Columns.Contains("PRICE")
				&& table.Columns.Contains("PRICEF")
				&& table.Columns.Contains("NDS")
				&& table.Columns.Contains("FIRM")
				&& table.Columns.Contains("EAN13")
				&& table.Columns.Contains("SERTIF")
				&& table.Columns.Contains("SERN")
				&& table.Columns.Contains("SERTDATE")
				&& table.Columns.Contains("SERTORG")
				&& table.Columns.Contains("APTEKA")
				&& table.Columns.Contains("SUMPAY");
		}
	}
}
