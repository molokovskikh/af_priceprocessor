using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Models;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class AptekaHoldingVolgogradParser : BaseDbfParser2
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "TTN")
				.DocumentHeader(h => h.DocumentDate, "TTN_DATE")
				.Invoice(i => i.InvoiceNumber, "I_NUM", "TTN")
				.Invoice(i => i.InvoiceDate, "I_DATE", "TTN_DATE")
				.Invoice(i => i.SellerName, "I_SEL_NAME")
				.Invoice(i => i.SellerAddress, "I_SEL_ADR")
				.Invoice(i => i.SellerINN, "I_SEL_INN")
				.Invoice(i => i.SellerKPP, "I_SEL_KPP")
				.Invoice(i => i.ShipperInfo, "I_SHIP_ADR")
				.Invoice(i => i.RecipientId, "I_RES_ID")
				.Invoice(i => i.RecipientName, "I_RES_NAME")
				.Invoice(i => i.RecipientAddress, "ANAME_AF", "I_RES_ADR")
				.Invoice(i => i.BuyerId, "I_BU_ID")
				.Invoice(i => i.BuyerName, "I_BU_NAME")
				.Invoice(i => i.BuyerAddress, "I_BU_ADR")
				.Invoice(i => i.BuyerINN, "I_BU_INN")
				.Invoice(i => i.BuyerKPP, "I_BU_KPP")
				.Invoice(i => i.DateOfPaymentDelay, "I_DEL_D")
				.Invoice(i => i.AmountWithoutNDS0, "amnt_e_0")
				.Invoice(i => i.AmountWithoutNDS10, "amnt_e_10")
				.Invoice(i => i.AmountWithoutNDS18, "amnt_e_18")
				.Invoice(i => i.Amount18, "amnt_w_18")
				.Invoice(i => i.Amount10, "amnt_w_10")
				.Invoice(i => i.NDSAmount, "amnt_n_all")
				.Invoice(i => i.NDSAmount10, "amnt_n_10")
				.Invoice(i => i.NDSAmount18, "amnt_n_18")
				.Invoice(i => i.AmountWithoutNDS, "amnt_e_all")
				.Invoice(i => i.Amount, "amnt")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.Unit, "UNIT")
				.Line(l => l.Period, "SGODN")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.CertificatesDate, "SERT_DATE")
				.Line(l => l.ProducerCostWithoutNDS, "PRCENA_BND", "PRCENABNDS")
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
				.Line(l => l.BillOfEntryNumber, "BLL_NTR_ID")
				.Line(l => l.Producer, "PRZV_POST")
				.Line(l => l.Product, "NAME_POST")
				.Line(l => l.Code, "SP_PRD_ID")
				.Line(l => l.OrderId, "N_ZAK");
		}
	}
}
