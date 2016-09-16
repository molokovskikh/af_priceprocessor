using System.Data;
using System.Windows.Forms.VisualStyles;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class TekoSParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(h => h.ProviderDocumentId, "TTN")
				.DocumentHeader(h => h.DocumentDate, "TTN_DATE")
				.Line(l => l.Product, "NAME_POST")
				.Line(l => l.Producer, "PRZV_POST")
				.Line(l => l.SerialNumber, "SERIA")
				.Line(l => l.Period, "SGODN")
				.Line(l => l.Certificates, "SERT")
				.Line(l => l.ProducerCostWithoutNDS, "PRCENABNDS")
				.Line(l => l.SupplierCostWithoutNDS, "PCENA_BNDS")
				.Line(l => l.Nds, "NDS")
				.Line(l => l.SupplierCost, "PCENA_NDS")
				.Line(l => l.Quantity, "KOL_TOV")
				.Line(l => l.CodeCr, "SP_PRDR_ID", "SP_PRD_ID")
				.Line(l => l.VitallyImportant, "VT")
				.Line(l => l.NdsAmount, "P_NDS_AMNT")
				.Line(l => l.Amount, "P_AMNT")
				.Line(l => l.Unit, "UNIT")
				.Line(l => l.BillOfEntryNumber, "BLL_NTR_ID")
				.Line(l => l.EAN13, "BAR_CODE")
				.Line(l => l.CertificatesEndDate, "SERT_END")
				.Line(l => l.RegistryCost, "GR_CENA")
				.Line(l => l.DateOfManufacture, "MAN_DATE")
				.Line(l => l.Country, "PRZV_CNTR")
				.Line(l => l.RegistryDate, "REG_DATE")
				.Line(l => l.CertificateAuthority, "SERT_AUTH")
				.Line(l => l.CertificatesDate, "SERT_DATE")

				.Invoice(i => i.InvoiceNumber, "I_NUM")
				.Invoice(i => i.InvoiceDate, "I_DATE")
				.Invoice(i => i.SellerAddress, "I_SEL_ADR")
				.Invoice(i => i.SellerINN, "I_SEL_INN")
				.Invoice(i => i.SellerKPP, "I_SEL_KPP")
				.Invoice(i => i.RecipientName, "I_RES_NAME")
				.Invoice(i => i.RecipientId, "I_RES_ID")
				.Invoice(i => i.ShipperInfo, "I_RES_ADR")
				.Invoice(i => i.BuyerId, "I_BU_ID")
				.Invoice(i => i.BuyerName, "I_BU_NAME")
				.Invoice(i => i.BuyerAddress, "I_BU_ADR")
				.Invoice(i => i.BuyerINN, "I_BU_INN")
				.Invoice(i => i.BuyerKPP, "I_BU_KPP")
				.Invoice(i => i.SellerName, "I_SEL_NAME")
				.Invoice(i => i.Amount, "AMNT")
				.Invoice(i => i.NDSAmount, "AMNT_N_ALL")
				.Invoice(i => i.DelayOfPaymentInDays, "I_DEL_D");
		}

		public static bool CheckFileFormat(DataTable data)
		{
			var ProductIndex = data.Columns.Contains("NAME_POST");
			var SupplierCostIndex = data.Columns.Contains("PCENA_NDS");
			var SupplierCostWithoutNdsIndex = data.Columns.Contains("PCENA_BNDS");
			var NdsIndex = data.Columns.Contains("NDS");
			var RecipientNameIndex = data.Columns.Contains("I_RES_NAME");
			var CertificatesEndDateIndex = data.Columns.Contains("SERT_END");

			if (!ProductIndex || !RecipientNameIndex || !CertificatesEndDateIndex)
				return false;

			if (SupplierCostIndex && SupplierCostWithoutNdsIndex)
				return true;
			if (SupplierCostIndex && NdsIndex)
				return true;
			if (SupplierCostWithoutNdsIndex && NdsIndex)
				return true;
			return false;
		}
	}
}
