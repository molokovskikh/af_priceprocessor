using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class AptekaHoldingVolgogradParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "TTN")
				.DocumentHeader(h => h.DocumentDate, "TTN_DATE")
				.DocumentInvoice(i => i.RecipientAddress, "ANAME_AF")
				.DocumentInvoice(i => i.InvoiceNumber, "TTN")
				.DocumentInvoice(i => i.InvoiceDate, "TTN_DATE")
				.DocumentInvoice(i => i.SellerAddress, "I_SEL_ADR")
				.DocumentInvoice(i => i.SellerINN, "I_SEL_INN")
				.DocumentInvoice(i => i.SellerKPP, "I_SEL_KPP")
				.DocumentInvoice(i => i.SellerName, "I_SEL_NAME")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.Period, "SGODN")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.CertificatesDate, "SERT_DATE")
				.Line(l => l.ProducerCostWithoutNDS, "PRCENA_BND")
				.Line(l => l.RegistryCost, "GR_CENA")
				.Line(l => l.SupplierCostWithoutNDS, "PCENA_BNDS")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.SupplierCost, "PCENA_NDS")
				.Line(l => l.Quantity, "KOL_TOV")
				.Line(l => l.SupplierPriceMarkup, "SP_MARKUP")
				.Line(l => l.NdsAmount, "P_NDS_AMNT")
				.Line(l => l.Amount, "P_AMNT")
				.Line(l => l.CertificateAuthority, "SERT_AUTH")
				.Line(l => l.VitallyImportant, "VT")
				.Line(l => l.EAN13, "BAR_CODE", "EAN13")
				.Line(l => l.Producer, "PRZV_POST")
				.Line(l => l.Product, "NAME_POST")
				.Line(l => l.Code, "SP_PRD_ID");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("PRCENA_BND") &&
				data.Columns.Contains("TTN") &&
				data.Columns.Contains("TTN_DATE") &&
				data.Columns.Contains("PRZV_POST");
		}
	}
}
