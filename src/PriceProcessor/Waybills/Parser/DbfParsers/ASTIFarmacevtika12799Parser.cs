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
				.Line(l => l.Code, "CodeTov")//CodeTov = Код товара
				.Line(l => l.Product, "TovName")//TovName = Наименование
				.Line(l => l.Producer, "PrName")//PrName = Производитель
				.Line(l => l.Country, "PrStrana")//PrStrana = Страна Производителя
				.Line(l => l.Unit, "EdIzm")//EdIzm = ЕдиницаИзмерения
				.Line(l => l.Quantity, "Kol")//Kol = Количество в накл.
				.Line(l => l.SupplierCostWithoutNDS, "CwoNDS")//CwoNDS = Цена без НДС  Поставщика
				.Line(l => l.SupplierCost, "CwNDS")//CwNDS = Цена с НДС Поставщика
				.Line(l => l.ProducerCostWithoutNDS, "CPwoNDS")//CPwoNDS = Цена Производителя
				.Line(l => l.ProducerCost, "CPwNDS")//CPwNDS = Цена Производителя С НДС - Цена производителя с НДС (не маппится, используется для доп. расчетов)???
				.Line(l => l.Nds, "StNDS")//StNDS = Ставка НДС;
				#region поля не на что мапить
				//.Line(l=>)//Sum = Сумма без НДС
				#endregion
				.Line(l => l.NdsAmount, "SumNDS")//SumNDS = Сумма НДС;
				.Line(l => l.Amount, "Vsego")//Vsego = Сумма по строке с НДС
				.Line(l => l.Period, "SrokGodn")//SrokGodn = Срок Годности
				.Line(l => l.SerialNumber, "Seriya")//Seriya = Серия
				.Line(l => l.BillOfEntryNumber, "GTD")//GTD = Номер ГТД = BillOfEntryNumber
				.Line(l => l.Certificates, "SertNom")//SertNom = Номер Сертификата
				.Line(l => l.CertificatesDate, "SertData")//SertData = Серт. выдан до
				#region поля не на что мапить
				//.Line(l=>l.)//SertOrg = Орг.-ция выдавшая серт-т ??? нет
				//.Line(l=>l.)//RegNom = Рег. номер;??? нет
				//.Line(l=>l.)//RegData = Дата регистрации??? нет
				//.Line()//RegVydan = Лаборатория??? нет
				#endregion
				.DocumentHeader(d => d.ProviderDocumentId, "NOMDOC")//NOMDOC = Номер накладной
				.DocumentHeader(d => d.DocumentDate, "DATDOC")//DATDOC = Дата накладной
				.DocumentInvoice(i => i.BuyerAddress, "TO")//.Line()//TO = Адрес аптеки??? есть - инвойс получатель
				#region поле не на что мапить
				//.Line()//CenaFZ = Цена оптовой орг-ции, закупившей у пр-ля (1-го звена) С НДС;??? нет
				#endregion
				.Line(l=>l.SupplierPriceMarkup, "Proc")//Proc = Процент наценки (над ценой пр-ля)
				.Line(l=>l.RegistryCost, "Creestr")//Creestr = Цена реестра (в данный момент заменяется на Зарегистрированную, т.е. тоже самое что и CZareg)
				#region поле дублируется
				//.Line(l=>l)//GN = Если ЖизненноНеобходимый=1 тогда "*" иначе "";
				#endregion
				.Line(l => l.VitallyImportant, "GN2")//GN2 - числовое обозначение ЖВ	- использовать это
				#region поле совпадает с Creestr
				//.Line(l=>l.RegistryCost, "CZareg")//CZareg = Зарегистрированная Цена производителя; нет пишем как цена реестра - совпадает с колонкой CREESTR, но колонка CREESTR не отображена в требовании
				#endregion
				.Line(l => l.EAN13, "EAN");//EAN - Штрихкод
				#region поле не на что мапить
				//.Line()//NAPT - код аптеки
				#endregion
		}
		public static bool CheckFileFormat(DataTable data)
		{
			var columns = data.Columns;
			return columns.Contains("CodeTov")
				&& columns.Contains("TovName")
				&& columns.Contains("PrName")
				&& columns.Contains("PrStrana")
				&& columns.Contains("EdIzm")
				&& columns.Contains("Kol")
				&& columns.Contains("CwoNDS")
				&& columns.Contains("CwNDS")
				&& columns.Contains("CPwoNDS")
				&& columns.Contains("CPwNDS")
				&& columns.Contains("StNDS")
				#region
				//&& columns.Contains("Sum")
				#endregion
				&& columns.Contains("SumNDS")
				&& columns.Contains("Vsego")
				&& columns.Contains("SrokGodn")
				&& columns.Contains("Seriya")
				&& columns.Contains("GTD")
				&& columns.Contains("SertNom")
				&& columns.Contains("SertData")
				#region
				//&& columns.Contains("SertOrg")
				//&& columns.Contains("RegNom")
				//&& columns.Contains("RegData")
				//&& columns.Contains("RegVydan")
				#endregion
				&& columns.Contains("NOMDOC")
				&& columns.Contains("DATDOC")
				&& columns.Contains("TO")
				#region
				//&& columns.Contains("CenaFZ")
				#endregion
				&& columns.Contains("Proc")
				&& columns.Contains("Creestr")
				#region
				//&& columns.Contains("GN")
				#endregion
				&& columns.Contains("GN2")
				#region
				//&& columns.Contains("CZareg")
				#endregion
				&& columns.Contains("EAN");
				#region
				//&& columns.Contains("NAPT")
				#endregion
		}
	}
}
