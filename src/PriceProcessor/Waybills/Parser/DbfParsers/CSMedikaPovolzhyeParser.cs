using System;
using System.Data;
using System.Linq;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class CSMedikaPovolzhyeParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NOMER")
				.DocumentHeader(d => d.DocumentDate, "DATADOK")
				.DocumentInvoice(i => i.BuyerName, "KLI")
				.DocumentInvoice(i => i.RecipientAddress, "POLUCH")
				.Line(l => l.Code, "KOD")
				.Line(l => l.Product, "NM")
				.Line(l => l.Producer, "PROIZV")
				.Line(l => l.Quantity, "KOLVO")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.SupplierCost, "PRICEWNDS")
				.Line(l => l.NdsAmount, "SUMMANDS")
				.Line(l => l.Amount, "SUMMA")
				.Line(l => l.Certificates, "SERTIF")
				.Line(l => l.CertificatesDate, "SERTSROK");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("KLI")
				&& !data.Columns.Contains("ZVNLS")
				&& data.Columns.Contains("PRICEWNDS")
				&& data.Columns.Contains("DATADOK")
				&& data.Columns.Contains("SERTSROK");
		}
	}
}