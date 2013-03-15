using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Models;
using NHibernate.Linq;
using log4net;

namespace Inforoom.PriceProcessor.Waybills.Models.Export
{
	public class DbfExporter
	{
		private static ILog _log = LogManager.GetLogger(typeof(DbfExporter));

		public static void SaveLessUniversal(Document document, string filename)
		{
			var table = new DataTable();

			table.Columns.AddRange(new[] {
				new DataColumn("postid_af"),
				new DataColumn("post_name_af"),
				new DataColumn("apt_af"),
				new DataColumn("aname_af"),
				new DataColumn("ttn"),
				new DataColumn("ttn_date"),
				new DataColumn("id_artis"),
				new DataColumn("name_artis"),
				new DataColumn("przv_artis"),
				new DataColumn("name_post"),
				new DataColumn("przv_post"),
				new DataColumn("seria"),
				new DataColumn("sgodn"),
				new DataColumn("sert"),
				new DataColumn("sert_date"),
				new DataColumn("prcena_bnds"),
				new DataColumn("gr_cena"),
				new DataColumn("pcena_bnds"),
				new DataColumn("nds"),
				new DataColumn("pcena_nds"),
				new DataColumn("kol_tov"),
				new DataColumn("ean13")
			});

			foreach (var line in document.Lines) {
				var row = table.NewRow();
				row["postid_af"] = document.FirmCode;
				row["post_name_af"] = document.Log.Supplier.FullName;
				row["apt_af"] = document.Address.Id;
				row["aname_af"] = document.Address.Name;
				row["ttn"] = document.ProviderDocumentId;
				row["ttn_date"] = document.DocumentDate;
				if (line.AssortimentPriceInfo != null && line.AssortimentPriceInfo.Code != null)
					row["id_artis"] = line.AssortimentPriceInfo.Code;
				if (line.AssortimentPriceInfo != null && line.AssortimentPriceInfo.Synonym != null)
					row["name_artis"] = line.AssortimentPriceInfo.Synonym;
				if (line.AssortimentPriceInfo != null && line.AssortimentPriceInfo.SynonymFirmCr != null)
					row["przv_artis"] = line.AssortimentPriceInfo.SynonymFirmCr;
				row["name_post"] = line.Product;
				row["przv_post"] = line.Producer;
				row["seria"] = line.SerialNumber;
				row["sgodn"] = line.Period;
				row["sert"] = line.Certificates;
				row["sert_date"] = line.CertificatesDate;
				row["prcena_bnds"] = line.ProducerCostWithoutNDS;
				row["gr_cena"] = line.RegistryCost;
				row["pcena_bnds"] = line.SupplierCostWithoutNDS;
				row["nds"] = line.Nds;
				row["pcena_nds"] = line.SupplierCost;
				row["kol_tov"] = line.Quantity;
				row["ean13"] = line.EAN13;

				table.Rows.Add(row);
			}

			Dbf.Save(table, filename);
		}

		public static void SaveUniversalDbf(Document document, string file)
		{
			var table = new DataTable();
			table.Columns.AddRange(new[] {
				new DataColumn("postid_af", typeof(int)),
				new DataColumn("post_name_af") { MaxLength = 255 },
				new DataColumn("apt_af", typeof(int)),
				new DataColumn("aname_af") { MaxLength = 255 },
				new DataColumn("ttn") { MaxLength = 50 },
				new DataColumn("ttn_date", typeof(DateTime)),
				new DataColumn("id_artis", typeof(int)),
				new DataColumn("name_artis") { MaxLength = 255 },
				new DataColumn("przv_artis") { MaxLength = 150 },
				new DataColumn("sp_prd_id") { MaxLength = 20 },
				new DataColumn("name_post") { MaxLength = 255 },
				new DataColumn("przv_post") { MaxLength = 255 },
				new DataColumn("seria") { MaxLength = 50 },
				new DataColumn("sgodn", typeof(DateTime)),
				new DataColumn("sert") { MaxLength = 150 },
				new DataColumn("sert_date", typeof(DateTime)),
				new DataColumn("prcena_bnds", typeof(decimal)) {
					ExtendedProperties = {
						{ "presision", 12 },
						{ "scale", 2 },
					}
				},
				new DataColumn("gr_cena", typeof(decimal)) {
					ExtendedProperties = {
						{ "presision", 12 },
						{ "scale", 2 },
					}
				},
				new DataColumn("pcena_bnds", typeof(decimal)) {
					ExtendedProperties = {
						{ "presision", 12 },
						{ "scale", 2 },
					}
				},
				new DataColumn("nds", typeof(int)),
				new DataColumn("pcena_nds", typeof(decimal)) {
					ExtendedProperties = {
						{ "presision", 12 },
						{ "scale", 2 },
					}
				},
				new DataColumn("kol_tov", typeof(decimal)) {
					ExtendedProperties = {
						{ "presision", 10 },
						{ "scale", 2 },
					}
				},
				//дополнительные поля
				new DataColumn("sp_markup", typeof(decimal)),
				new DataColumn("p_nds_amnt", typeof(decimal)),
				new DataColumn("p_amnt", typeof(decimal)),
				new DataColumn("sert_auth") { MaxLength = 255 },
				new DataColumn("reg_date", typeof(DateTime)),
				new DataColumn("vt", typeof(bool)),
				new DataColumn("unit", typeof(string)) { MaxLength = 20 },
				new DataColumn("prd_in_mn", typeof(int)),
				new DataColumn("excise_tx", typeof(decimal)),
				new DataColumn("bll_ntr_id", typeof(string)) { MaxLength = 30 },
				new DataColumn("bar_code") { MaxLength = 13 },
				new DataColumn("man_date", typeof(DateTime)),
				new DataColumn("i_num") { MaxLength = 20 },
				new DataColumn("i_date", typeof(DateTime)),
				new DataColumn("i_sel_name") { MaxLength = 255 },
				new DataColumn("i_sel_adr") { MaxLength = 255 },
				new DataColumn("i_sel_inn") { MaxLength = 20 },
				new DataColumn("i_sel_kpp") { MaxLength = 20 },
				new DataColumn("i_ship_adr") { MaxLength = 255 },
				new DataColumn("i_res_name") { MaxLength = 255 },
				new DataColumn("i_res_id", typeof(int)),
				new DataColumn("i_res_adr") { MaxLength = 255 },
				new DataColumn("i_doc_info") { MaxLength = 255 },
				new DataColumn("i_bu_id", typeof(int)),
				new DataColumn("i_bu_name") { MaxLength = 255 },
				new DataColumn("i_bu_adr") { MaxLength = 255 },
				new DataColumn("i_bu_inn") { MaxLength = 20 },
				new DataColumn("i_bu_kpp") { MaxLength = 20 },
				new DataColumn("amnt_e_0", typeof(decimal)),
				new DataColumn("amnt_w_10", typeof(decimal)),
				new DataColumn("amnt_e_10", typeof(decimal)),
				new DataColumn("amnt_n_10", typeof(decimal)),
				new DataColumn("amnt_w_18", typeof(decimal)),
				new DataColumn("amnt_e_18", typeof(decimal)),
				new DataColumn("amnt_n_18", typeof(decimal)),
				new DataColumn("amnt_n_all", typeof(decimal)),
				new DataColumn("amnt_e_all", typeof(decimal)),
				new DataColumn("amnt", typeof(decimal)),
				new DataColumn("i_del_d", typeof(int)),
				new DataColumn("i_del_bd", typeof(int)),
				new DataColumn("com_fee_id", typeof(string)) { MaxLength = 255 },
				new DataColumn("com_fee", typeof(decimal)),
				new DataColumn("shifr", typeof(string)) { MaxLength = 255 },
				new DataColumn("opt_cena", typeof(decimal)),
				new DataColumn("otp_cena", typeof(decimal)),
				new DataColumn("rcena", typeof(decimal)),
				new DataColumn("storename", typeof(string)) { MaxLength = 255 },
				new DataColumn("id_producer", typeof(string)) { MaxLength = 20 },
				new DataColumn("sp_producer_id", typeof(string)) { MaxLength = 20 },
			});

			var fixColumns = table.Columns.Cast<DataColumn>().Where(c => c.DataType == typeof(decimal) && !c.ExtendedProperties.ContainsKey("presision"));
			foreach (var column in fixColumns) {
				column.ExtendedProperties.Add("presision", 12);
				column.ExtendedProperties.Add("scale", 2);
			}

			foreach (var line in document.Lines) {
				var row = table.NewRow();
				row.SetField("postid_af", document.FirmCode);
				row.SetField("post_name_af", document.Log.Supplier.FullName);
				row.SetField("apt_af", document.Address.Id);
				row.SetField("aname_af", document.Address.Name);
				row.SetField("ttn", document.ProviderDocumentId);

				row.SetField("ttn_date", document.DocumentDate);
				if (line.AssortimentPriceInfo != null) {
					row.SetField("id_artis", line.AssortimentPriceInfo.Code);
					row.SetField("name_artis", line.AssortimentPriceInfo.Synonym);
					row.SetField("przv_artis", line.AssortimentPriceInfo.SynonymFirmCr);
					row.SetField("id_producer", line.AssortimentPriceInfo.CodeCr);
				}
				else {
					if(line.ProductEntity != null)
						row.SetField("name_artis", line.ProductEntity.CatalogProduct.Name);
					var producer = SessionHelper.WithSession(s => s.Query<Producer>().FirstOrDefault(p => p.Id == line.ProducerId));
					if(producer != null)
						row.SetField("przv_artis", producer.Name);
				}
				row.SetField("sp_prd_id", line.Code);
				row.SetField("name_post", line.Product);
				row.SetField("przv_post", line.Producer);
				row.SetField("seria", line.SerialNumber);

				DateTime period;
				DateTime? nullPeriod = null;
				if(DateTime.TryParse(line.Period, out period)) {
					nullPeriod = period;
				}
				row.SetField("sgodn", nullPeriod);

				row.SetField("prd_in_mn", line.ExpireInMonths);
				row.SetField("man_date", line.DateOfManufacture);

				row.SetField("sert", line.Certificates);
				row.SetField("sert_auth", line.CertificateAuthority);
				nullPeriod = null;
				if(DateTime.TryParse(line.CertificatesDate, out period)) {
					nullPeriod = period;
				}
				row.SetField("sert_date", nullPeriod);

				row.SetField("prcena_bnds", line.ProducerCostWithoutNDS);
				row.SetField("gr_cena", line.RegistryCost);
				row.SetField("reg_date", line.RegistryDate);
				row.SetField("sp_markup", line.SupplierPriceMarkup);
				row.SetField("pcena_bnds", line.SupplierCostWithoutNDS);
				row.SetField("pcena_nds", line.SupplierCost);
				row.SetField("kol_tov", line.Quantity);
				row.SetField("bar_code", line.EAN13);

				row.SetField("p_amnt", line.Amount);
				row.SetField("nds", line.Nds);
				row.SetField("p_nds_amnt", line.NdsAmount);

				row.SetField("unit", line.Unit);
				row.SetField("vt", line.VitallyImportant);
				row.SetField("excise_tx", line.ExciseTax);
				row.SetField("bll_ntr_id", line.BillOfEntryNumber);

				row.SetField("opt_cena", line.TradeCost);
				row.SetField("otp_cena", line.SaleCost);
				row.SetField("rcena", line.RetailCost);
				row.SetField("shifr", line.Cipher);
				row.SetField("sp_producer_id", line.CodeCr);

				var invoice = document.Invoice;
				if (invoice != null) {
					row.SetField("i_num", invoice.InvoiceNumber);
					row.SetField("i_date", invoice.InvoiceDate);
					row.SetField("i_sel_name", invoice.SellerName);
					row.SetField("i_sel_adr", invoice.SellerAddress);
					row.SetField("i_sel_inn", invoice.SellerINN);
					row.SetField("i_sel_kpp", invoice.SellerKPP);
					row.SetField("i_ship_adr", invoice.ShipperInfo);
					row.SetField("i_res_name", invoice.RecipientName);

					row.SetField("i_res_id", invoice.RecipientId);
					row.SetField("i_res_adr", invoice.RecipientAddress);
					row.SetField("i_doc_info", invoice.PaymentDocumentInfo);

					row.SetField("i_bu_id", invoice.BuyerId);
					row.SetField("i_bu_name", invoice.BuyerName);
					row.SetField("i_bu_adr", invoice.BuyerAddress);
					row.SetField("i_bu_inn", invoice.BuyerINN);
					row.SetField("i_bu_kpp", invoice.BuyerKPP);

					row.SetField("amnt_e_0", invoice.AmountWithoutNDS0);

					row.SetField("amnt_e_10", invoice.AmountWithoutNDS0);
					row.SetField("amnt_n_10", invoice.NDSAmount10);
					row.SetField("amnt_w_10", invoice.Amount10);

					row.SetField("amnt_e_18", invoice.AmountWithoutNDS18);
					row.SetField("amnt_n_18", invoice.NDSAmount18);
					row.SetField("amnt_w_18", invoice.Amount18);
					row.SetField("amnt_n_all", invoice.NDSAmount);
					row.SetField("amnt_e_all", invoice.AmountWithoutNDS);
					row.SetField("amnt", invoice.Amount);

					row.SetField("i_del_d", invoice.DelayOfPaymentInDays);
					row.SetField("i_del_bd", invoice.DelayOfPaymentInBankDays);

					row.SetField("com_fee_id", invoice.CommissionFeeContractId);
					row.SetField("com_fee", invoice.CommissionFee);

					row.SetField("storename", invoice.StoreName);
				}

				var columns = table.Columns.Cast<DataColumn>().Where(c => c.MaxLength != -1);
				foreach (var column in columns) {
					var value = row[column] as string;
					if (value != null
						&& value.Length > column.MaxLength) {
						row[column] = value.Slice(column.MaxLength);
					}
				}

				table.Rows.Add(row);
			}

			using (var writer = new StreamWriter(file, false, Encoding.GetEncoding(866)))
				Dbf2.Save(table, writer);
		}

		public static void SaveProtek(Document document, string filename)
		{
			var table = new DataTable();

			table.Columns.AddRange(new[] {
				new DataColumn("ID"),
				new DataColumn("Date", typeof(DateTime)),
				new DataColumn("Address"),
				new DataColumn("Org"),
				new DataColumn("SupID"),
				new DataColumn("SupDate", typeof(DateTime)),
				new DataColumn("Product"),
				new DataColumn("Code"),
				new DataColumn("Cert"),
				new DataColumn("CertDate"),
				new DataColumn("Period"),
				new DataColumn("Producer"),
				new DataColumn("Country"),
				new DataColumn("ProdCost", typeof(decimal)),
				new DataColumn("RegCost", typeof(decimal)),
				new DataColumn("CostMarkup", typeof(decimal)),
				new DataColumn("CostWoNds", typeof(decimal)),
				new DataColumn("CostWNds", typeof(decimal)),
				new DataColumn("Quantity", typeof(int)),
				new DataColumn("VI", typeof(bool)),
				new DataColumn("Nds", typeof(int)),
				new DataColumn("Seria"),
				new DataColumn("Sum", typeof(decimal)),
				new DataColumn("NdsSum", typeof(decimal)),
				new DataColumn("Unit"),
				new DataColumn("ExciseTax", typeof(decimal)),
				new DataColumn("Entry"),
				new DataColumn("Ean13")
			});

			foreach (var line in document.Lines) {
				var row = table.NewRow();
				row.SetField("ID", document.Log.Id);
				row.SetField("Date", document.Log.LogTime);
				row.SetField("Address", document.Address.Name);
				row.SetField("Org", document.Address.Org.FullName);
				row.SetField("SupID", document.ProviderDocumentId);
				row.SetField("SupDate", document.DocumentDate);
				row.SetField("Product", line.Product);
				row.SetField("Code", line.Code);
				row.SetField("Cert", line.Certificates);
				row.SetField("CertDate", line.CertificatesDate);
				row.SetField("Period", line.Period);
				row.SetField("Producer", line.Producer);
				row.SetField("Country", line.Country);
				row.SetField("ProdCost", line.ProducerCost);
				row.SetField("RegCost", line.RegistryCost);
				row.SetField("CostMarkup", line.SupplierPriceMarkup);
				row.SetField("CostWoNds", line.SupplierCostWithoutNDS);
				row.SetField("CostWNds", line.SupplierCost);
				row.SetField("Quantity", line.Quantity);
				row.SetField("VI", line.VitallyImportant);
				row.SetField("Nds", line.Nds);
				row.SetField("Seria", line.SerialNumber);
				row.SetField("Sum", line.Amount);
				row.SetField("NdsSum", line.NdsAmount);
				row.SetField("Unit", line.Unit);
				row.SetField("ExciseTax", line.ExciseTax);
				row.SetField("Entry", line.BillOfEntryNumber);
				row.SetField("Ean13", line.EAN13);
				table.Rows.Add(row);
			}

			Dbf.Save(table, filename);
		}
	}
}