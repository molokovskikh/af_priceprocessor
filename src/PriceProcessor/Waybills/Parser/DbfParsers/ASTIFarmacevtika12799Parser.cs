using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Inforoom.PriceProcessor.Waybills.Parser.DbfParsers
{
	public class ASTIFarmacevtika12799Parser : BaseDbfParser
	{
		public override DbfParser GetParser()
		{
			/*
				Описание полей
				CodeTov = Код товара
				TovName = Наименование
				PrName = Производитель
				PrStrana = Страна Производителя
				EdIzm = ЕдиницаИзмерения
				Kol = Количество в накл.
				CwoNDS = Цена без НДС
				CwNDS = Цена с НДС
				CPwoNDS = Цена Производителя
				CPwNDS = Цена Производителя С НДС;
				StNDS = Ставка НДС;
				Sum = Сумма по строке;
				SumNDS = Сумма НДС;
				Vsego = Сумма по строке с НДС;
				SrokGodn = Срок Годности
				Seriya = Серия
				GTD = Номер ГТД
				SertNom = Номер Сертификата
				SertData = Серт. выдан до
				SertOrg = Орг.-ция выдавшая серт-т
				RegNom = Рег. номер;
				RegData = Дата регистрации
				RegVydan = Лаборатория
				NOMDOC = Номер накладной
				DATDOC = Дата накладной
				TO = Адрес аптеки
				CenaFZ = Цена оптовой орг-ции, закупившей у пр-ля (1-го звена) С НДС;
				Proc = Процент наценки (над ценой пр-ля)
				Creestr = Цена реестра (в данный момент заменяется на Зарегистрированную, т.е. тоже самое что и CZareg);
				GN = Если ЖизненноНеобходимый=1 тогда "*" иначе "";
				GN2 - числовое обозначение ЖВ
				CZareg = Зарегистрированная Цена производителя;
				EAN - Штрихкод
				NAPT - код аптеки
			*/

			return new DbfParser()
				.DocumentHeader(d => d.ProviderDocumentId, "NOMDOC") //NOMDOC = Номер накладной
				.DocumentHeader(d => d.DocumentDate, "DATDOC") //DATDOC = Дата накладной
				.Invoice(i => i.BuyerAddress, "TO") //.Line()//TO = Адрес аптеки??? есть - инвойс получатель
				.Line(l => l.Code, "CodeTov") //CodeTov = Код товара
				.Line(l => l.Product, "TovName") //TovName = Наименование
				.Line(l => l.Producer, "PrName") //PrName = Производитель
				.Line(l => l.Country, "PrStrana") //PrStrana = Страна Производителя
				.Line(l => l.Unit, "EdIzm") //EdIzm = ЕдиницаИзмерения
				.Line(l => l.Quantity, "Kol") //Kol = Количество в накл.
				.Line(l => l.SupplierCostWithoutNDS, "CwoNDS") //CwoNDS = Цена без НДС  Поставщика
				.Line(l => l.SupplierCost, "CwNDS") //CwNDS = Цена с НДС Поставщика
				.Line(l => l.ProducerCostWithoutNDS, "CPwoNDS") //CPwoNDS = Цена Производителя
				.Line(l => l.ProducerCost, "CPwNDS") //CPwNDS = Цена Производителя С НДС - Цена производителя с НДС (не маппится, используется для доп. расчетов)???
				.Line(l => l.Nds, "StNDS") //StNDS = Ставка НДС;
				.Line(l => l.NdsAmount, "SumNDS") //SumNDS = Сумма НДС;
				.Line(l => l.Amount, "Vsego") //Vsego = Сумма по строке с НДС
				.Line(l => l.Period, "SrokGodn") //SrokGodn = Срок Годности
				.Line(l => l.SerialNumber, "Seriya") //Seriya = Серия
				.Line(l => l.BillOfEntryNumber, "GTD") //GTD = Номер ГТД = BillOfEntryNumber
				.Line(l => l.Certificates, "SertNom") //SertNom = Номер Сертификата
				.Line(l => l.CertificatesDate, "SertData") //SertData = Серт. выдан до
				.Line(l => l.CertificateAuthority, "SertOrg") //SertOrg = Орг.-ция выдавшая серт-т
				.Line(l => l.SupplierPriceMarkup, "Proc") //Proc = Процент наценки (над ценой пр-ля)
				.Line(l => l.RegistryCost, "Creestr") //Creestr = Цена реестра (в данный момент заменяется на Зарегистрированную, т.е. тоже самое что и CZareg)
				.Line(l => l.VitallyImportant, "GN2") //GN2 - числовое обозначение ЖВ	- использовать это
				.Line(l => l.EAN13, "EAN"); //EAN - Штрихкод
		}

		public static bool CheckFileFormat(DataTable data)
		{
			var columns = data.Columns;
			return columns.Contains("PrStrana")
				&& columns.Contains("EdIzm")
				&& columns.Contains("CwoNDS")
				&& columns.Contains("CwNDS")
				&& columns.Contains("CPwoNDS")
				&& columns.Contains("CPwNDS")
				&& columns.Contains("StNDS")
				&& columns.Contains("Creestr")
				&& columns.Contains("NAPT");
		}
	}
}