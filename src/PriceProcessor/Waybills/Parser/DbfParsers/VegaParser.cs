using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class VegaParser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "i_num") //номер счета фактуры
				.DocumentHeader(d => d.DocumentDate, "i_date ") //дата счета фактуры
				.Line(l => l.Code, "sp_prd_id ") //код товара поставщика
				.Line(l => l.Quantity, "kol_tov ") //Заказанное кол-во товара
				.Line(l => l.SupplierCost, "pcena_nds ") //Цена поставщика с НДС
				.Line(l => l.ProducerCostWithoutNDS, "prcena_bnd") //Цена производителя без НДС
				.Line(l => l.Nds, "nds ") //НДС
				.Line(l => l.NdsAmount, "p_nds_amnt") //Сумма НДС
				.Line(l => l.Amount, "p_amnt") //Сумма с НДС
				.Line(l => l.SupplierCostWithoutNDS, "pcena_bnds") //Цена поставщика без НДС
				.Line(l => l.SupplierPriceMarkup, "sp_markup") //Наценка поставщика
				.Line(l => l.Period, "sgodn") //Срок годности товара
				.Line(l => l.SerialNumber, "seria") //Серия товара
				.Line(l => l.BillOfEntryNumber, "bll_ntr_id") //Таможенной декларации
				.Line(l => l.Certificates, "sert") // Сертификат товара
				.Line(l => l.CertificateAuthority, "sert_auth") // Орган, выдавший документа качества
				.Line(l => l.Product, "name_post") // Наименование товара по справочнику поставщика
				.Line(l => l.Producer, "przv_post") // Производитель и страна производителя товара по справочнику поставщика
				.Line(l => l.EAN13, "bar_code ") //Код EAN-13 (штрих-код)
				.Line(l => l.VitallyImportant, "vt ") //Признак ЖНВЛС
				.Line(l => l.RegistryCost, "gr_cena"); //Цена по госреестру
		}

		public static bool CheckFileFormat(DataTable data)
		{
			var cols = data.Columns;
			var isTrue = cols.Contains("sp_prd_id ") && cols.Contains("i_bu_id ");
			return isTrue;
		}
	}
}