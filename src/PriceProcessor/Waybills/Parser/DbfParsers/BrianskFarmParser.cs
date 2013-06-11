using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class BrianskFarmParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "ttn")
				.DocumentHeader(d => d.DocumentDate, "ttn_date")
				.DocumentInvoice(i => i.InvoiceNumber, "i_num")
				.DocumentInvoice(i => i.InvoiceDate, "i_date")
				.DocumentInvoice(i => i.SellerName, "i_sel_name")
				.DocumentInvoice(i => i.SellerAddress, "i_sel_adr")
				.DocumentInvoice(i => i.SellerINN, "i_sel_inn")
				.DocumentInvoice(i => i.SellerKPP, "i_sel_kpp")
				.DocumentInvoice(i => i.BuyerName, "i_bu_name")
				.DocumentInvoice(i => i.BuyerAddress, "i_bu_adr")
				.DocumentInvoice(i => i.BuyerINN, "i_bu_inn")
				.DocumentInvoice(i => i.BuyerKPP, "i_bu_kpp")
				.DocumentInvoice(i => i.AmountWithoutNDS0, "amnt_e_0")
				.DocumentInvoice(i => i.AmountWithoutNDS10, "amnt_e_10")
				.DocumentInvoice(i => i.AmountWithoutNDS18, "amnt_e_18")
				.DocumentInvoice(i => i.Amount18, "amnt_w_18")
				.DocumentInvoice(i => i.Amount10, "amnt_w_10")
				.DocumentInvoice(i => i.NDSAmount, "amnt_n_all")
				.DocumentInvoice(i => i.NDSAmount10, "amnt_n_10")
				.DocumentInvoice(i => i.NDSAmount18, "amnt_n_18")
				.DocumentInvoice(i => i.AmountWithoutNDS, "amnt_e_all")
				.DocumentInvoice(i => i.Amount, "amnt")
				.Line(l => l.Product, "name_post")
				.Line(l => l.Producer, "przv_post")
				.Line(l => l.Quantity, "kol_tov")
				.Line(l => l.Period, "sgodn")
				.Line(l => l.SupplierCost, "pcena_nds")
				.Line(l => l.SupplierCostWithoutNDS, "pcena_bnds")
				.Line(l => l.ProducerCostWithoutNDS, "PRCENABNDS")
				.Line(l => l.RegistryCost, "gr_cena")
				.Line(l => l.SerialNumber, "seria")
				.Line(l => l.Certificates, "sert")
				.Line(l => l.CertificatesDate, "sert_date")
				.Line(l => l.CertificateAuthority, "sert_auth")
				.Line(l => l.Code, "sp_prd_id")
				.Line(l => l.Nds, "nds")
				.Line(l => l.SupplierPriceMarkup, "sp_markup")
				.Line(l => l.VitallyImportant, "vt")
				.Line(l => l.NdsAmount, "p_nds_amnt")
				.Line(l => l.Amount, "p_amnt")
				.Line(l => l.Unit, "unit")
				.Line(l => l.BillOfEntryNumber, "bll_ntr_id")
				.Line(l => l.EAN13, "bar_code");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			return data.Columns.Contains("pcena_bnds")
				&& data.Columns.Contains("bll_ntr_id")
				&& data.Columns.Contains("p_nds_amnt")
				&& data.Columns.Contains("amnt_e_all")
				&& data.Columns.Contains("sp_markup");
		}
	}
}
